# Testing Background Jobs

Background jobs are Hangfire-scheduled tasks that run on a recurring schedule. They do not receive HTTP requests — they are invoked by the Hangfire server at a configured time (e.g., daily at 01:00 UTC). The most important job in this project is `GenerateRecurringTransactionsJob`.

---

## What Is a Background Job?

A background job is a plain C# class whose name ends with `Job`. It has a public `ExecuteAsync` method that Hangfire calls when the schedule fires.

**Location:** `src/Kakeibo.Api/Features/{Domain}/{Op}Job.cs`

**Example:**

```csharp
public sealed class GenerateRecurringTransactionsJob(
    AppDbContext db,
    RecordTransactionHandler recordHandler,
    IEventBus eventBus,
    IClock clock,
    ILogger<GenerateRecurringTransactionsJob> logger)
{
    public async Task ExecuteAsync()
    {
        var today = clock.GetCurrentInstant().InUtc().Date;
        var patterns = await db.RecurringPatterns
            .Where(r => r.DeletedAt == null && r.NextOccurrence <= today)
            .ToListAsync();

        foreach (var pattern in patterns)
        {
            try
            {
                await ProcessPatternAsync(pattern, today);
            }
            catch (Exception ex)
            {
                logger.PatternProcessingFailed(pattern.Id, ex);
                // Continue processing remaining patterns
            }
        }
    }
}
```

**Key behaviors to test:**
- Patterns with `NextOccurrence <= today` are processed.
- Patterns with `NextOccurrence > today` are skipped.
- A transaction is created for each due occurrence.
- `NextOccurrence` is updated after each occurrence.
- A `RecurringTransactionGeneratedEvent` is published for each created transaction.
- A failure on one pattern does not prevent the others from being processed.

---

## How to Test a Background Job

You do not need Hangfire infrastructure. You do not need a running Hangfire server or a Hangfire storage backend. Simply call `ExecuteAsync()` directly.

### Step 1 — Obtain a real database context

```csharp
await using var db = await TestDbContextFactory.CreateAsync();
var ct = TestContext.Current.CancellationToken;
```

### Step 2 — Mock or instantiate dependencies

Mock services that have external side effects. Use a `FakeClock` to control what "today" is.

```csharp
var eventBus = Substitute.For<IEventBus>();
var logger = Substitute.For<ILogger<GenerateRecurringTransactionsJob>>();

// Fix the clock so "today" is deterministic
var today = LocalDate.FromDateTime(new DateTime(2024, 6, 15));
var clock = new FakeClock(Instant.FromUtc(2024, 6, 15, 1, 0, 0));
```

### Step 3 — Seed recurring patterns

Create `RecurringPattern` entities with `NextOccurrence` values that are before, equal to, or after "today" to exercise all branches.

```csharp
var user = SeedUser(db);
var wallet = SeedWallet(db, user.Id);
var category = SeedCategory(db, user.Id);

// Due today — should be processed
var duePattern = new RecurringPattern
{
    Id = Guid7.NewGuid().ToGuid(),
    UserId = user.Id,
    WalletId = wallet.Id,
    CategoryId = category.Id,
    Type = TransactionType.Expense,
    Amount = 9.99m,
    Description = "Spotify",
    Frequency = RecurrenceFrequency.Monthly,
    NextOccurrence = today,
    StartDate = today.PlusMonths(-1)
};

// Not due yet — should be skipped
var futurePattern = new RecurringPattern
{
    Id = Guid7.NewGuid().ToGuid(),
    UserId = user.Id,
    WalletId = wallet.Id,
    CategoryId = category.Id,
    Type = TransactionType.Expense,
    Amount = 1200m,
    Description = "Rent",
    Frequency = RecurrenceFrequency.Monthly,
    NextOccurrence = today.PlusDays(15)
};

db.RecurringPatterns.AddRange(duePattern, futurePattern);
await db.SaveChangesAsync(ct);
```

### Step 4 — Instantiate and call the job

When the job internally calls another handler (e.g., `RecordTransactionHandler`), you can either inject a real handler (with the same real DB context) or a substitute. Using a real handler is preferred because it tests the full flow.

```csharp
var recordHandler = new RecordTransactionHandler(db, eventBus, clock);

var job = new GenerateRecurringTransactionsJob(db, recordHandler, eventBus, clock, logger);
await job.ExecuteAsync();
```

### Step 5 — Assert results

```csharp
// The due pattern should have generated a transaction
var transactions = await db.Transactions
    .Where(t => t.UserId == user.Id && t.Description == "Spotify")
    .ToListAsync(ct);
Assert.Single(transactions);

// NextOccurrence should have advanced by one month
var updatedPattern = await db.RecurringPatterns.FindAsync([duePattern.Id], ct);
Assert.Equal(today.PlusMonths(1), updatedPattern!.NextOccurrence);

// The future pattern should be untouched
var unchangedPattern = await db.RecurringPatterns.FindAsync([futurePattern.Id], ct);
Assert.Equal(today.PlusDays(15), unchangedPattern!.NextOccurrence);

// An event was published for the generated transaction
eventBus.Received(1).Publish(Arg.Is<RecurringTransactionGeneratedEvent>(e =>
    e.UserId == user.Id));

// No event published for the future (skipped) pattern
// (verified implicitly by Received(1) above — only 1 event total)
```

---

## Testing Partial Progress (Error Isolation)

The job commits one occurrence at a time. If one pattern fails, the others should still be processed. Test this explicitly.

```csharp
[Fact]
public async Task ExecuteAsync_OnePatternFails_OtherPatternsStillProcessed()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;
    var clock = new FakeClock(Instant.FromUtc(2024, 6, 15, 1, 0, 0));
    var today = LocalDate.FromDateTime(new DateTime(2024, 6, 15));

    var user = SeedUser(db);
    var wallet = SeedWallet(db, user.Id);
    var category = SeedCategory(db, user.Id);

    // A pattern with a bad wallet reference (will cause an error)
    var brokenPattern = new RecurringPattern
    {
        Id = Guid7.NewGuid().ToGuid(),
        UserId = user.Id,
        WalletId = Guid.NewGuid(),    // non-existent wallet — will fail
        CategoryId = category.Id,
        Amount = 50m,
        NextOccurrence = today
    };

    // A valid pattern
    var goodPattern = new RecurringPattern
    {
        Id = Guid7.NewGuid().ToGuid(),
        UserId = user.Id,
        WalletId = wallet.Id,
        CategoryId = category.Id,
        Amount = 9.99m,
        NextOccurrence = today
    };

    db.RecurringPatterns.AddRange(brokenPattern, goodPattern);
    await db.SaveChangesAsync(ct);

    var eventBus = Substitute.For<IEventBus>();
    var logger = Substitute.For<ILogger<GenerateRecurringTransactionsJob>>();
    var recordHandler = new RecordTransactionHandler(db, eventBus, clock);
    var job = new GenerateRecurringTransactionsJob(db, recordHandler, eventBus, clock, logger);

    // Act — should NOT throw even though one pattern fails
    await job.ExecuteAsync();

    // The good pattern was still processed
    var goodTransactions = await db.Transactions
        .Where(t => t.WalletId == wallet.Id)
        .ToListAsync(ct);
    Assert.Single(goodTransactions);
}
```

---

## Testing Multiple Overdue Occurrences

If a job was not run for several days, a pattern may be overdue by multiple occurrences. The job should generate all of them in one run.

```csharp
[Fact]
public async Task ExecuteAsync_PatternOverdueByTwoMonths_GeneratesTwoTransactions()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;
    var today = LocalDate.FromDateTime(new DateTime(2024, 6, 15));
    var clock = new FakeClock(Instant.FromUtc(2024, 6, 15, 1, 0, 0));

    var user = SeedUser(db);
    var wallet = SeedWallet(db, user.Id);
    var category = SeedCategory(db, user.Id);

    // NextOccurrence is two months in the past
    var overduePattern = new RecurringPattern
    {
        UserId = user.Id,
        WalletId = wallet.Id,
        CategoryId = category.Id,
        Amount = 9.99m,
        Frequency = RecurrenceFrequency.Monthly,
        NextOccurrence = today.PlusMonths(-2)    // April 15
    };
    db.RecurringPatterns.Add(overduePattern);
    await db.SaveChangesAsync(ct);

    var eventBus = Substitute.For<IEventBus>();
    var logger = Substitute.For<ILogger<GenerateRecurringTransactionsJob>>();
    var recordHandler = new RecordTransactionHandler(db, eventBus, clock);
    var job = new GenerateRecurringTransactionsJob(db, recordHandler, eventBus, clock, logger);

    await job.ExecuteAsync();

    // Should have generated: April 15, May 15 = 2 transactions
    var transactions = await db.Transactions
        .Where(t => t.WalletId == wallet.Id)
        .ToListAsync(ct);
    Assert.Equal(2, transactions.Count);

    // NextOccurrence should now be July 15 (one month after the last processed date)
    var updated = await db.RecurringPatterns.FindAsync([overduePattern.Id], ct);
    Assert.Equal(today.PlusMonths(1), updated!.NextOccurrence);
}
```

---

## Checklist Before Submitting Background Job Tests

- [ ] Patterns due today are processed (transaction created, `NextOccurrence` updated)
- [ ] Patterns not yet due are skipped
- [ ] Correct event published for each generated transaction
- [ ] One pattern failure does not prevent other patterns from being processed
- [ ] Overdue patterns generate all missing occurrences
- [ ] `FakeClock` used to make "today" deterministic
- [ ] Test names follow `ExecuteAsync_{Scenario}_{ExpectedResult}` convention
- [ ] Docker skip guard inherited from `TestDbContextFactory`
