using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Kakeibo.Api.Features.Budgets.Events;
using Kakeibo.Api.Features.Transactions.Events;

namespace Kakeibo.Tests.Features.Budgets.Events;

public sealed class TransactionDeletedBudgetHandlerTests
{
    private static readonly Guid HousingId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static async Task<User> CreateUserAsync(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            Username = $"user_{Guid.NewGuid():N}"[..12],
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

    private static async Task<Transaction> CreateTransactionAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId,
        Guid walletId,
        Guid categoryId,
        decimal amount,
        LocalDate date)
    {
        var transaction = new Transaction
        {
            UserId = userId,
            WalletId = walletId,
            CategoryId = categoryId,
            Type = TransactionType.Expense,
            Amount = amount,
            Date = date,
            Description = "Test",
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction;
    }

    [Fact]
    public async Task IgnoresNonExpenseTransactions()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new TransactionDeletedBudgetHandler(db);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m);
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 100m;
        await db.SaveChangesAsync(ct);

        // Deleting an income — should not affect budget
        await handler.HandleAsync(new TransactionDeletedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 100m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        var refreshed = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(100m, refreshed!.CurrentSpending);
    }

    [Fact]
    public async Task RecalculatesSpending_FromDatabase()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new TransactionDeletedBudgetHandler(db);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m);

        // Seed two transactions (simulating prior recording)
        var t1 = await CreateTransactionAsync(db, user.Id, wallet.Id, HousingId, 100m, new LocalDate(2026, 6, 1));
        var t2 = await CreateTransactionAsync(db, user.Id, wallet.Id, HousingId, 80m, new LocalDate(2026, 7, 1));

        // Set CurrentSpending to reflect both transactions
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 180m;
        await db.SaveChangesAsync(ct);

        // Soft-delete t2 before firing the event
        t2.DeletedAt = SystemClock.Instance.GetCurrentInstant();
        await db.SaveChangesAsync(ct);

        // Fire delete event for t2 — handler should recalculate from remaining transactions
        await handler.HandleAsync(new TransactionDeletedEvent
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
    public async Task HandlesNoMatchingBudgets_Gracefully()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new TransactionDeletedBudgetHandler(db);

        // No budgets for this user — should complete without error
        await handler.HandleAsync(new TransactionDeletedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 50m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);
    }
}
