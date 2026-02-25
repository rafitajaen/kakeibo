using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Recurring.Jobs;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Kakeibo.Tests.Features.Recurring.Jobs;

public sealed class GenerateRecurringTransactionsJobTests
{
    // Clock fixed at 2026-03-15 (mid-month, easy to reason about)
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 15, 1, 0));

    private static IEventBus CreateFakeEventBus() => Substitute.For<IEventBus>();
    private static ILogger<GenerateRecurringTransactionsJob> CreateFakeLogger() =>
        Substitute.For<ILogger<GenerateRecurringTransactionsJob>>();

    private static async Task<(User user, Wallet wallet, WalletBalance balance, Category category)> SetupAsync(
        Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);

        var wallet = new Wallet { Name = "Wallet", OwnerId = user.Id, Currency = "EUR" };
        db.Wallets.Add(wallet);

        var balance = new WalletBalance { WalletId = wallet.Id, Balance = 1000m };
        db.WalletBalances.Add(balance);

        // System category (no userId) — accessible to all
        var category = new Category { Name = "Housing", UserId = null };
        db.Categories.Add(category);

        await db.SaveChangesAsync();
        return (user, wallet, balance, category);
    }

    private static RecurringPattern CreatePattern(Guid userId, Guid walletId, Guid categoryId,
        LocalDate nextOccurrence, LocalDate? endDate = null, Instant? deletedAt = null,
        RecurrenceFrequency frequency = RecurrenceFrequency.Monthly)
    {
        return new RecurringPattern
        {
            UserId = userId,
            Name = "Rent",
            Amount = 100m,
            Description = "Monthly rent",
            TransactionType = TransactionType.Expense,
            Frequency = frequency,
            CategoryId = categoryId,
            WalletId = walletId,
            StartDate = new LocalDate(2026, 1, 1),
            NextOccurrence = nextOccurrence,
            EndDate = endDate,
            DeletedAt = deletedAt
        };
    }

    [Fact]
    public async Task PatternDueToday_GeneratesTransactionAndAdvancesNextOccurrence()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, _, category) = await SetupAsync(db);

        var pattern = CreatePattern(user.Id, wallet.Id, category.Id,
            nextOccurrence: new LocalDate(2026, 3, 15)); // Due today
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var recordHandler = new RecordTransactionHandler(db, CreateFakeEventBus(), TestClock);
        var job = new GenerateRecurringTransactionsJob(db, recordHandler, CreateFakeEventBus(), TestClock, CreateFakeLogger());

        await job.ExecuteAsync();

        // Transaction should be created
        var transactions = await db.Transactions.Where(t => t.UserId == user.Id).ToListAsync(ct);
        Assert.Single(transactions);

        // NextOccurrence should advance to 2026-04-15
        var updatedPattern = await db.RecurringPatterns.FirstAsync(r => r.Id == pattern.Id, ct);
        Assert.Equal(new LocalDate(2026, 4, 15), updatedPattern.NextOccurrence);
        Assert.NotNull(updatedPattern.LastGeneratedAt);
    }

    [Fact]
    public async Task FuturePattern_IsSkipped()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, _, category) = await SetupAsync(db);

        var pattern = CreatePattern(user.Id, wallet.Id, category.Id,
            nextOccurrence: new LocalDate(2026, 4, 1)); // Future — should not run today
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var recordHandler = new RecordTransactionHandler(db, CreateFakeEventBus(), TestClock);
        var job = new GenerateRecurringTransactionsJob(db, recordHandler, CreateFakeEventBus(), TestClock, CreateFakeLogger());

        await job.ExecuteAsync();

        var transactions = await db.Transactions.Where(t => t.UserId == user.Id).ToListAsync(ct);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task ExpiredPattern_EndDateInPast_IsSkipped()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, _, category) = await SetupAsync(db);

        // NextOccurrence is today but EndDate is in the past — pattern expired
        var pattern = CreatePattern(user.Id, wallet.Id, category.Id,
            nextOccurrence: new LocalDate(2026, 3, 15),
            endDate: new LocalDate(2026, 3, 1)); // EndDate before NextOccurrence
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var recordHandler = new RecordTransactionHandler(db, CreateFakeEventBus(), TestClock);
        var job = new GenerateRecurringTransactionsJob(db, recordHandler, CreateFakeEventBus(), TestClock, CreateFakeLogger());

        await job.ExecuteAsync();

        var transactions = await db.Transactions.Where(t => t.UserId == user.Id).ToListAsync(ct);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task SoftDeletedPattern_IsSkipped()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, _, category) = await SetupAsync(db);

        var pattern = CreatePattern(user.Id, wallet.Id, category.Id,
            nextOccurrence: new LocalDate(2026, 3, 15),
            deletedAt: Instant.FromUtc(2026, 3, 1, 0, 0));
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var recordHandler = new RecordTransactionHandler(db, CreateFakeEventBus(), TestClock);
        var job = new GenerateRecurringTransactionsJob(db, recordHandler, CreateFakeEventBus(), TestClock, CreateFakeLogger());

        await job.ExecuteAsync();

        var transactions = await db.Transactions.Where(t => t.UserId == user.Id).ToListAsync(ct);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task ThreeMissedDays_GeneratesThreeTransactions()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, _, category) = await SetupAsync(db);

        // Daily pattern missed 3 days (Mar 13, 14, 15 — today = Mar 15)
        var pattern = CreatePattern(user.Id, wallet.Id, category.Id,
            nextOccurrence: new LocalDate(2026, 3, 13),
            frequency: RecurrenceFrequency.Daily);
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var recordHandler = new RecordTransactionHandler(db, CreateFakeEventBus(), TestClock);
        var job = new GenerateRecurringTransactionsJob(db, recordHandler, CreateFakeEventBus(), TestClock, CreateFakeLogger());

        await job.ExecuteAsync();

        var transactions = await db.Transactions.Where(t => t.UserId == user.Id).ToListAsync(ct);
        Assert.Equal(3, transactions.Count);

        // NextOccurrence should be advanced past today: Mar 16
        var updatedPattern = await db.RecurringPatterns.FirstAsync(r => r.Id == pattern.Id, ct);
        Assert.Equal(new LocalDate(2026, 3, 16), updatedPattern.NextOccurrence);
    }

    [Fact]
    public async Task Idempotency_RunningTwiceSameDay_GeneratesOnlyOneTransaction()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, _, category) = await SetupAsync(db);

        var pattern = CreatePattern(user.Id, wallet.Id, category.Id,
            nextOccurrence: new LocalDate(2026, 3, 15)); // Due today (monthly)
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var recordHandler = new RecordTransactionHandler(db, CreateFakeEventBus(), TestClock);
        var job = new GenerateRecurringTransactionsJob(db, recordHandler, CreateFakeEventBus(), TestClock, CreateFakeLogger());

        // Run twice on the same day
        await job.ExecuteAsync();
        await job.ExecuteAsync();

        // Monthly pattern: NextOccurrence advances to Apr 15 after first run,
        // so second run finds nothing due on Mar 15 (or today)
        var transactions = await db.Transactions.Where(t => t.UserId == user.Id).ToListAsync(ct);
        Assert.Single(transactions);
    }
}
