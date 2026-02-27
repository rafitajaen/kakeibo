using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Kakeibo.Api.Features.Budgets.Events;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Budgets.Events;

public sealed class TransactionUpdatedBudgetHandlerTests
{
    private static readonly Guid HousingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FoodId = Guid.Parse("10000000-0000-0000-0000-000000000003");

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
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId,
        Guid categoryId,
        decimal limit)
    {
        var handler = new CreateBudgetHandler(db);
        var result = await handler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", categoryId, limit, "2026-01-01", "2026-12-31", null),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    private static async Task<Transaction> CreateExpenseTransactionAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId,
        Guid walletId,
        Guid categoryId,
        decimal amount,
        LocalDate date)
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
    public async Task IgnoresNonExpenseTransactions()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionUpdatedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m);
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 50m;
        await db.SaveChangesAsync(ct);

        // Income update — should not affect budget
        await handler.HandleAsync(new TransactionUpdatedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 100m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
            OldCategoryId = HousingId,
            OldDate = new LocalDate(2026, 6, 1),
            OldAmount = 80m,
        }, ct);

        var refreshed = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(50m, refreshed!.CurrentSpending);
    }

    [Fact]
    public async Task RecalculatesSpending_WhenAmountChanges()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionUpdatedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m);

        // Existing transaction with amount 100; it is now updated to 150
        var t = await CreateExpenseTransactionAsync(db, user.Id, wallet.Id, HousingId, 150m, new LocalDate(2026, 6, 1));

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 100m; // stale
        await db.SaveChangesAsync(ct);

        await handler.HandleAsync(new TransactionUpdatedEvent
        {
            TransactionId = t.Id,
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 150m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
            OldCategoryId = HousingId,
            OldDate = new LocalDate(2026, 6, 1),
            OldAmount = 100m,
        }, ct);

        var refreshed = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(150m, refreshed!.CurrentSpending);
    }

    [Fact]
    public async Task RecalculatesSpending_WhenCategoryChanges()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionUpdatedBudgetHandler(db, eventBus);

        // Two budgets — one for Housing, one for Food
        var housingBudgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m);
        var foodBudgetId = await CreateBudgetAsync(db, user.Id, FoodId, 300m);

        // Transaction originally in Housing, now moved to Food
        var t = await CreateExpenseTransactionAsync(db, user.Id, wallet.Id, FoodId, 100m, new LocalDate(2026, 6, 1));

        // Set stale spending for both budgets
        var housingBudget = await db.Budgets.FindAsync([housingBudgetId], ct);
        housingBudget!.CurrentSpending = 200m; // had transaction there before
        var foodBudget = await db.Budgets.FindAsync([foodBudgetId], ct);
        foodBudget!.CurrentSpending = 0m;
        await db.SaveChangesAsync(ct);

        await handler.HandleAsync(new TransactionUpdatedEvent
        {
            TransactionId = t.Id,
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 100m,
            CategoryId = FoodId,      // new category
            Date = new LocalDate(2026, 6, 1),
            OldCategoryId = HousingId, // old category
            OldDate = new LocalDate(2026, 6, 1),
            OldAmount = 100m,
        }, ct);

        // Housing budget should now show 0 (no Housing transactions exist)
        var refreshedHousing = await db.Budgets.FindAsync([housingBudgetId], ct);
        Assert.Equal(0m, refreshedHousing!.CurrentSpending);

        // Food budget should now show 100 (transaction moved here)
        var refreshedFood = await db.Budgets.FindAsync([foodBudgetId], ct);
        Assert.Equal(100m, refreshedFood!.CurrentSpending);
    }

    [Fact]
    public async Task PublishesAlertEvents_WhenThresholdCrossed()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionUpdatedBudgetHandler(db, eventBus);

        // Budget with limit 100; spending was 50 (below 75%)
        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 100m);
        // Transaction already in DB with new amount 110 (will trigger both thresholds)
        await CreateExpenseTransactionAsync(db, user.Id, wallet.Id, HousingId, 110m, new LocalDate(2026, 6, 1));

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 50m; // stale — below 75%
        await db.SaveChangesAsync(ct);

        await handler.HandleAsync(new TransactionUpdatedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 110m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
            OldCategoryId = HousingId,
            OldDate = new LocalDate(2026, 6, 1),
            OldAmount = 50m,
        }, ct);

        eventBus.Received(1).Publish(Arg.Any<BudgetWarningEvent>());
        eventBus.Received(1).Publish(Arg.Any<BudgetExceededEvent>());
    }
}
