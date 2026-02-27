using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Kakeibo.Api.Features.Budgets.Events;
using Kakeibo.Api.Features.Budgets.GetBudgetStatus;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Integration.Budgets;

/// <summary>
/// Integration tests for Phase 4b Budget Monitoring: event-driven spending updates,
/// recalculation on delete/update, and threshold alert publishing.
/// </summary>
public sealed class BudgetMonitoringTests
{
    private static readonly Guid HousingId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static async Task<User> CreateUserAsync(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Wallet> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });
        await db.SaveChangesAsync();
        return wallet;
    }

    private static async Task<Guid> CreateBudgetAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid userId, decimal limit)
    {
        var handler = new CreateBudgetHandler(db);
        var result = await handler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", HousingId, limit, "2026-01-01", "2026-12-31", null),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    private static async Task<Transaction> SeedExpenseAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId, Guid walletId, Guid categoryId,
        decimal amount, LocalDate date)
    {
        var t = new Transaction
        {
            UserId = userId,
            WalletId = walletId,
            CategoryId = categoryId,
            Type = TransactionType.Expense,
            Amount = amount,
            Date = date,
            Description = "Test",
        };
        db.Transactions.Add(t);
        await db.SaveChangesAsync();
        return t;
    }

    [Fact]
    public async Task RecordExpense_UpdatesBudgetSpending()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();

        var budgetId = await CreateBudgetAsync(db, user.Id, 500m);
        var recordHandler = new TransactionRecordedBudgetHandler(db, eventBus);

        await recordHandler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 120m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(120m, budget!.CurrentSpending);
    }

    [Fact]
    public async Task DeleteTransaction_RecalculatesSpending()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();

        var budgetId = await CreateBudgetAsync(db, user.Id, 500m);

        // Seed two transactions
        var t1 = await SeedExpenseAsync(db, user.Id, wallet.Id, HousingId, 100m, new LocalDate(2026, 6, 1));
        var t2 = await SeedExpenseAsync(db, user.Id, wallet.Id, HousingId, 80m, new LocalDate(2026, 7, 1));

        // Set spending to reflect both
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 180m;
        await db.SaveChangesAsync(ct);

        // Soft-delete t2
        t2.DeletedAt = SystemClock.Instance.GetCurrentInstant();
        await db.SaveChangesAsync(ct);

        var deleteHandler = new TransactionDeletedBudgetHandler(db);
        await deleteHandler.HandleAsync(new TransactionDeletedEvent
        {
            TransactionId = t2.Id,
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 80m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 7, 1),
        }, ct);

        var refreshed = await db.Budgets.FindAsync([budgetId], ct);
        // Only t1 (100) remains
        Assert.Equal(100m, refreshed!.CurrentSpending);
    }

    [Fact]
    public async Task UpdateTransaction_RecalculatesSpending()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();

        var budgetId = await CreateBudgetAsync(db, user.Id, 500m);

        // Transaction in DB at updated amount
        var t = await SeedExpenseAsync(db, user.Id, wallet.Id, HousingId, 200m, new LocalDate(2026, 6, 1));

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 100m; // stale
        await db.SaveChangesAsync(ct);

        var updateHandler = new TransactionUpdatedBudgetHandler(db, eventBus);
        await updateHandler.HandleAsync(new TransactionUpdatedEvent
        {
            TransactionId = t.Id,
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 200m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
            OldCategoryId = HousingId,
            OldDate = new LocalDate(2026, 6, 1),
            OldAmount = 100m,
        }, ct);

        var refreshed = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(200m, refreshed!.CurrentSpending);
    }

    [Fact]
    public async Task ThresholdCrossing_PublishesBudgetWarningEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();

        // Budget of 100; spending starts at 60 (below 75%)
        var budgetId = await CreateBudgetAsync(db, user.Id, 100m);
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 60m;
        await db.SaveChangesAsync(ct);

        var recordHandler = new TransactionRecordedBudgetHandler(db, eventBus);

        // Expense of 20 pushes spending to 80 (above 75%)
        await recordHandler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 20m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        eventBus.Received(1).Publish(Arg.Any<BudgetWarningEvent>());
        eventBus.DidNotReceive().Publish(Arg.Any<BudgetExceededEvent>());
    }

    [Fact]
    public async Task ThresholdCrossing_PublishesBudgetExceededEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();

        // Budget of 100; spending at 90 (above 75%, below 100%)
        var budgetId = await CreateBudgetAsync(db, user.Id, 100m);
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 90m;
        await db.SaveChangesAsync(ct);

        var recordHandler = new TransactionRecordedBudgetHandler(db, eventBus);

        // Expense of 20 pushes to 110 (above 100%)
        await recordHandler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 20m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        eventBus.DidNotReceive().Publish(Arg.Any<BudgetWarningEvent>()); // already past 75%
        eventBus.Received(1).Publish(Arg.Any<BudgetExceededEvent>());
    }

    [Fact]
    public async Task GetBudgetStatus_ReturnsCorrectStatus_AfterSpendingUpdate()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();

        var budgetId = await CreateBudgetAsync(db, user.Id, 100m);

        // Record expense pushing to Warning status (80%)
        var recordHandler = new TransactionRecordedBudgetHandler(db, eventBus);
        await recordHandler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 80m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        var statusHandler = new GetBudgetStatusHandler(db);
        var status = await statusHandler.HandleAsync(budgetId, user.Id, ct);

        Assert.True(status.IsSuccess);
        Assert.Equal("Warning", status.Value.Status);
        Assert.Equal(80m, status.Value.CurrentSpending);
        Assert.Equal(80m, status.Value.PercentageUsed);
        Assert.Equal(20m, status.Value.Remaining);
    }
}
