# Testing Event Handlers

Event handlers react to domain events published asynchronously via `IEventBus`. They update the database, publish downstream events, and run entirely in the background — the original caller never waits for them.

---

## What Is an Event Handler?

An event handler is a class that implements `IEventHandler<TEvent>`. It has a single method, `HandleAsync`, that receives a domain event and performs side effects such as updating database records or publishing further events.

**Location:** `src/Kakeibo.Api/Features/{Domain}/Events/` (in the *consuming* domain)

**Example:**

```csharp
// In Features/Goals/Events/TransactionRecordedGoalHandler.cs
public sealed class TransactionRecordedGoalHandler(AppDbContext db, IEventBus eventBus, IClock clock)
    : IEventHandler<TransactionRecordedEvent>
{
    public async Task HandleAsync(TransactionRecordedEvent @event, CancellationToken ct = default)
    {
        // Find goals linked to the wallet in the event
        // Update CurrentProgress on each goal
        // Publish GoalMilestoneReachedEvent or GoalAchievedEvent if thresholds crossed
        await db.SaveChangesAsync(ct);
    }
}
```

---

## How the Event System Works

Understanding the flow helps you write correct tests:

1. A handler calls `eventBus.Publish(new TransactionRecordedEvent { ... })`.
2. `ChannelEventBus` enqueues the event into a `Channel<IEvent>`.
3. `EventDispatcher` (a `BackgroundService`) reads from the channel in the background.
4. `EventDispatcher` creates a new DI scope, resolves all `IEventHandler<TransactionRecordedEvent>` instances, and calls `HandleAsync` on each one sequentially.

**In tests, you bypass steps 2, 3, and 4 entirely.** You call `HandleAsync` directly on the event handler instance. This keeps tests fast, deterministic, and independent of the background infrastructure.

---

## How to Test an Event Handler

Testing an event handler is very similar to testing a handler: instantiate it directly with a real database context and call `HandleAsync` with a crafted event.

### Step 1 — Obtain a real database context

```csharp
await using var db = await TestDbContextFactory.CreateAsync();
```

### Step 2 — Seed the data the handler will query

The handler will look up entities by IDs from the event. Those entities must exist in the database.

```csharp
var user = new User { Id = Guid7.NewGuid().ToGuid(), Email = "alice@example.com", /* ... */ };
var wallet = new Wallet { Id = Guid7.NewGuid().ToGuid(), UserId = user.Id, /* ... */ };
var goal = new Goal
{
    Id = Guid7.NewGuid().ToGuid(),
    UserId = user.Id,
    WalletId = wallet.Id,
    TargetAmount = 1000m,
    CurrentProgress = 0m
};
db.Users.Add(user);
db.Wallets.Add(wallet);
db.Goals.Add(goal);
await db.SaveChangesAsync(ct);
```

### Step 3 — Mock the downstream event bus

If the handler publishes further events (e.g., `GoalMilestoneReachedEvent`), mock `IEventBus` to capture them.

```csharp
var eventBus = Substitute.For<IEventBus>();
```

### Step 4 — Instantiate and call the handler

```csharp
var handler = new TransactionRecordedGoalHandler(db, eventBus, SystemClock.Instance);

var @event = new TransactionRecordedEvent
{
    TransactionId = Guid.NewGuid(),
    WalletId = wallet.Id,
    UserId = user.Id,
    Type = "Income",
    Amount = 250m,
    CategoryId = Guid.NewGuid(),
    Date = LocalDate.FromDateTime(DateTime.UtcNow)
};

await handler.HandleAsync(@event, TestContext.Current.CancellationToken);
```

### Step 5 — Assert database state

After calling `HandleAsync`, the handler will have called `SaveChangesAsync`. Query the database to verify the updated state.

```csharp
var updatedGoal = await db.Goals.FindAsync([goal.Id], ct);
Assert.NotNull(updatedGoal);
Assert.Equal(250m, updatedGoal.CurrentProgress);
```

### Step 6 — Assert downstream events

```csharp
// No milestone yet (250 / 1000 = 25%, which is the first milestone)
eventBus.Received(1).Publish(Arg.Is<GoalMilestoneReachedEvent>(e =>
    e.GoalId == goal.Id && e.MilestonePercent == 25));
```

---

## Complete Example

```csharp
public sealed class TransactionRecordedGoalHandlerTests
{
    [Fact]
    public async Task HandleAsync_IncomeInLinkedWallet_UpdatesGoalProgress()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var user = new User
        {
            Id = Guid7.NewGuid().ToGuid(),
            Email = "alice@example.com",
            Username = "alice",
            PasswordHash = "hash",
            Currency = "EUR"
        };
        var wallet = new Wallet
        {
            Id = Guid7.NewGuid().ToGuid(),
            UserId = user.Id,
            Name = "Vacation Fund",
            Type = WalletType.Personal,
            Currency = "EUR"
        };
        var goal = new Goal
        {
            Id = Guid7.NewGuid().ToGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Name = "Europe Trip",
            TargetAmount = 1000m,
            CurrentProgress = 0m,
            TrackingMode = GoalTrackingMode.WalletLinked
        };
        db.Users.Add(user);
        db.Wallets.Add(wallet);
        db.Goals.Add(goal);
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus, SystemClock.Instance);
        var @event = new TransactionRecordedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = SystemClock.Instance.GetCurrentInstant(),
            TransactionId = Guid.NewGuid(),
            WalletId = wallet.Id,
            UserId = user.Id,
            Type = "Income",
            Amount = 250m,
            CategoryId = Guid.NewGuid(),
            Date = LocalDate.FromDateTime(DateTime.UtcNow)
        };

        // Act
        await handler.HandleAsync(@event, ct);

        // Assert — goal progress updated
        var updatedGoal = await db.Goals.FindAsync([goal.Id], ct);
        Assert.NotNull(updatedGoal);
        Assert.Equal(250m, updatedGoal.CurrentProgress);

        // 250/1000 = 25% — first milestone event should fire
        eventBus.Received(1).Publish(Arg.Is<GoalMilestoneReachedEvent>(e =>
            e.GoalId == goal.Id && e.MilestonePercent == 25));
    }

    [Fact]
    public async Task HandleAsync_TransactionInUnlinkedWallet_DoesNotUpdateGoal()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var user = new User { Id = Guid7.NewGuid().ToGuid(), Email = "bob@example.com", /* ... */ };
        var linkedWallet = new Wallet { Id = Guid7.NewGuid().ToGuid(), UserId = user.Id, /* ... */ };
        var otherWallet = new Wallet { Id = Guid7.NewGuid().ToGuid(), UserId = user.Id, /* ... */ };
        var goal = new Goal
        {
            Id = Guid7.NewGuid().ToGuid(),
            UserId = user.Id,
            WalletId = linkedWallet.Id,    // linked to linkedWallet, NOT otherWallet
            TargetAmount = 1000m,
            CurrentProgress = 0m,
            TrackingMode = GoalTrackingMode.WalletLinked
        };
        db.Users.Add(user);
        db.Wallets.AddRange(linkedWallet, otherWallet);
        db.Goals.Add(goal);
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus, SystemClock.Instance);
        var @event = new TransactionRecordedEvent
        {
            WalletId = otherWallet.Id,    // event is for the OTHER wallet
            Amount = 500m,
            /* ... */
        };

        await handler.HandleAsync(@event, ct);

        // Goal should be unchanged
        var unchanged = await db.Goals.FindAsync([goal.Id], ct);
        Assert.Equal(0m, unchanged!.CurrentProgress);

        // No milestone event should have fired
        eventBus.DidNotReceive().Publish(Arg.Any<GoalMilestoneReachedEvent>());
    }
}
```

---

## Key Patterns for Event Handler Tests

### Pattern 1 — Test no-op scenarios

Always verify that the handler does nothing when the event does not apply. This guards against handlers that accidentally modify unrelated data.

### Pattern 2 — Test milestone boundaries

If the handler emits milestone events (e.g., at 25%, 50%, 75%, 100% progress), write a separate test for each boundary. Start the goal progress at just below the milestone and send a transaction that crosses it.

### Pattern 3 — Test idempotency (if applicable)

Some handlers are designed to be safe to run twice for the same event. If yours is, add a test that calls `HandleAsync` twice with the same event and verifies the state is only applied once.

---

## What You Do NOT Need

- You do not need `EventDispatcher` — call `HandleAsync` directly.
- You do not need a running Hangfire server.
- You do not need the full ASP.NET Core pipeline.
- You do not need `WebApplicationFactory`.

---

## Checklist Before Submitting Event Handler Tests

- [ ] Happy path: correct DB state after handler runs
- [ ] No-op path: handler ignores events that don't apply to it
- [ ] Milestone/threshold paths tested for handlers that publish downstream events
- [ ] Downstream event publication verified with `eventBus.Received()`
- [ ] No downstream events on no-op path verified with `eventBus.DidNotReceive()`
- [ ] Test names follow `HandleAsync_{Scenario}_{ExpectedResult}` convention
