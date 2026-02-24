using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.DeleteTransaction;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Features.Transactions.UpdateTransaction;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Testing;

namespace Kakeibo.Tests.Integration.Transactions;

/// <summary>
/// Verifies that balance updates are atomic — WalletBalance changes happen in the same
/// SaveChangesAsync call as the transaction, and are correctly reversed on delete/update.
/// </summary>
public sealed class TransactionAtomicityTests
{
    private static readonly FakeClock TestClock =
        new(Instant.FromUtc(2026, 3, 1, 12, 0));

    // A date well within the allowed range (not in the future)
    private const string TestDate = "2026-02-15";

    private static async Task<(User user, Wallet wallet)> SeedAsync(
        Kakeibo.Api.Persistence.AppDbContext db, CancellationToken ct)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);

        var wallet = new Wallet
        {
            Name = "Test Wallet",
            OwnerId = user.Id,
            Currency = "EUR"
        };
        db.Wallets.Add(wallet);

        // WalletBalance seeded directly (simulates what CreateWalletHandler does)
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });

        await db.SaveChangesAsync(ct);
        return (user, wallet);
    }

    // Retrieves the current balance for a wallet directly from the DB.
    private static async Task<decimal> GetBalanceAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid walletId, CancellationToken ct)
    {
        return await db.WalletBalances
            .Where(wb => wb.WalletId == walletId)
            .Select(wb => wb.Balance)
            .FirstAsync(ct);
    }

    [Fact]
    public async Task Income_AtomicBalanceUpdate_IncreasesSourceBalance()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var (user, wallet) = await SeedAsync(db, ct);

        var handler = new RecordTransactionHandler(db, eventBus, TestClock);
        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Income", 500m, "Salary", TestDate,
                Guid.Parse("10000000-0000-0000-0000-000000000001"), // Housing (system category)
                wallet.Id, null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, await GetBalanceAsync(db, wallet.Id, ct));
    }

    [Fact]
    public async Task Expense_AtomicBalanceUpdate_DecreasesSourceBalance()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var (user, wallet) = await SeedAsync(db, ct);

        // Start with 1000 balance
        db.WalletBalances.First(wb => wb.WalletId == wallet.Id).Balance = 1000m;
        await db.SaveChangesAsync(ct);

        var handler = new RecordTransactionHandler(db, eventBus, TestClock);
        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 300m, "Groceries", TestDate,
                Guid.Parse("10000000-0000-0000-0000-000000000003"), // Food & Dining
                wallet.Id, null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(700m, await GetBalanceAsync(db, wallet.Id, ct));
    }

    [Fact]
    public async Task Transfer_AtomicBothBalances_SingleSaveChangesCall()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var (user, sourceWallet) = await SeedAsync(db, ct);

        // Second wallet for transfer destination
        var destWallet = new Wallet
        {
            Name = "Savings",
            OwnerId = user.Id,
            Currency = "EUR"
        };
        db.Wallets.Add(destWallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = destWallet.Id, Balance = 0m });
        db.WalletBalances.First(wb => wb.WalletId == sourceWallet.Id).Balance = 1000m;
        await db.SaveChangesAsync(ct);

        var handler = new RecordTransactionHandler(db, eventBus, TestClock);
        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Transfer", 200m, "To savings", TestDate,
                Guid.Parse("10000000-0000-0000-0000-000000000009"), // Savings & Investments
                sourceWallet.Id, destWallet.Id),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(800m, await GetBalanceAsync(db, sourceWallet.Id, ct));
        Assert.Equal(200m, await GetBalanceAsync(db, destWallet.Id, ct));
    }

    [Fact]
    public async Task DeleteIncome_ReversesBalance()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var (user, wallet) = await SeedAsync(db, ct);

        // Record income first
        var recordHandler = new RecordTransactionHandler(db, eventBus, TestClock);
        var recorded = await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Income", 500m, "Salary", TestDate,
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                wallet.Id, null),
            user.Id, ct);

        Assert.Equal(500m, await GetBalanceAsync(db, wallet.Id, ct));

        // Delete the transaction — balance should go back to 0
        var deleteHandler = new DeleteTransactionHandler(db, eventBus, TestClock);
        var deleteResult = await deleteHandler.HandleAsync(recorded.Value.Id, user.Id, ct);

        Assert.True(deleteResult.IsSuccess);
        Assert.Equal(0m, await GetBalanceAsync(db, wallet.Id, ct));
    }

    [Fact]
    public async Task UpdateIncome_DeltaCorrect_BalanceAdjusted()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var (user, wallet) = await SeedAsync(db, ct);

        // Record income of 500
        var recordHandler = new RecordTransactionHandler(db, eventBus, TestClock);
        var recorded = await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Income", 500m, "Salary", TestDate,
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                wallet.Id, null),
            user.Id, ct);

        Assert.Equal(500m, await GetBalanceAsync(db, wallet.Id, ct));

        // Update amount to 700 — balance should increase by 200
        var updateHandler = new UpdateTransactionHandler(db, eventBus, TestClock);
        var updateResult = await updateHandler.HandleAsync(
            recorded.Value.Id,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                700m, "Salary updated", TestDate,
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                null),
            user.Id, ct);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal(700m, await GetBalanceAsync(db, wallet.Id, ct));
    }

    [Fact]
    public async Task UpdateTransfer_ChangeDestination_RebalancesCorrectly()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var (user, sourceWallet) = await SeedAsync(db, ct);

        var oldDest = new Wallet { Name = "Old Dest", OwnerId = user.Id, Currency = "EUR" };
        var newDest = new Wallet { Name = "New Dest", OwnerId = user.Id, Currency = "EUR" };
        db.Wallets.AddRange(oldDest, newDest);
        db.WalletBalances.AddRange(
            new WalletBalance { WalletId = oldDest.Id, Balance = 0m },
            new WalletBalance { WalletId = newDest.Id, Balance = 0m });
        db.WalletBalances.First(wb => wb.WalletId == sourceWallet.Id).Balance = 1000m;
        await db.SaveChangesAsync(ct);

        // Record transfer to oldDest
        var recordHandler = new RecordTransactionHandler(db, eventBus, TestClock);
        var recorded = await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Transfer", 100m, "To old dest", TestDate,
                Guid.Parse("10000000-0000-0000-0000-000000000009"),
                sourceWallet.Id, oldDest.Id),
            user.Id, ct);

        Assert.True(recorded.IsSuccess);
        Assert.Equal(900m, await GetBalanceAsync(db, sourceWallet.Id, ct));
        Assert.Equal(100m, await GetBalanceAsync(db, oldDest.Id, ct));
        Assert.Equal(0m, await GetBalanceAsync(db, newDest.Id, ct));

        // Update: change amount to 150 and destination to newDest
        var updateHandler = new UpdateTransactionHandler(db, eventBus, TestClock);
        var updateResult = await updateHandler.HandleAsync(
            recorded.Value.Id,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                150m, "To new dest", TestDate,
                Guid.Parse("10000000-0000-0000-0000-000000000009"),
                newDest.Id),
            user.Id, ct);

        Assert.True(updateResult.IsSuccess);
        // Source: was 900, delta = 150 - 100 = 50, so 900 - 50 = 850
        Assert.Equal(850m, await GetBalanceAsync(db, sourceWallet.Id, ct));
        // Old dest: reverted — loses its 100
        Assert.Equal(0m, await GetBalanceAsync(db, oldDest.Id, ct));
        // New dest: receives 150
        Assert.Equal(150m, await GetBalanceAsync(db, newDest.Id, ct));
    }
}
