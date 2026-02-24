using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.DeleteTransaction;
using Kakeibo.Api.Features.Transactions.GetTransaction;
using Kakeibo.Api.Features.Transactions.ListTransactions;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Features.Transactions.UpdateTransaction;
using Kakeibo.Api.Features.Wallets.CreateWallet;
using Kakeibo.Api.Features.Wallets.GetWallet;
using Kakeibo.Api.Infrastructure.Events;
using NodaTime;
using NodaTime.Testing;

namespace Kakeibo.Tests.Integration.Transactions;

/// <summary>
/// Integration tests for the Transaction CRUD lifecycle, access control,
/// and cross-domain wallet balance integration.
/// </summary>
public sealed class TransactionCrudTests
{
    private static readonly FakeClock TestClock =
        new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private const string TestDate = "2026-02-15";

    // System category IDs from HasData seed
    private static readonly Guid HousingCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FoodCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    private static async Task<User> CreateUserAsync(
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
        await db.SaveChangesAsync(ct);
        return user;
    }

    private static async Task<Wallet> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, User user, CancellationToken ct, string name = "Checking")
    {
        var wallet = new Wallet
        {
            Name = name,
            OwnerId = user.Id,
            Currency = "EUR"
        };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 500m });
        await db.SaveChangesAsync(ct);
        return wallet;
    }

    [Fact]
    public async Task FullLifecycle_RecordListGetUpdateDelete()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var user = await CreateUserAsync(db, ct);
        var wallet = await CreateWalletAsync(db, user, ct);

        var recordHandler = new RecordTransactionHandler(db, eventBus, TestClock);
        var listHandler = new ListTransactionsHandler(db);
        var getHandler = new GetTransactionHandler(db);
        var updateHandler = new UpdateTransactionHandler(db, eventBus, TestClock);
        var deleteHandler = new DeleteTransactionHandler(db, eventBus, TestClock);

        // --- RECORD ---
        var recorded = await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 100m, "Rent", TestDate, HousingCategoryId, wallet.Id, null),
            user.Id, ct);

        Assert.True(recorded.IsSuccess);
        var txId = recorded.Value.Id;
        Assert.Equal("Expense", recorded.Value.Type);
        Assert.Equal(100m, recorded.Value.Amount);

        // --- LIST ---
        var listed = await listHandler.HandleAsync(wallet.Id, user.Id, 1, 50, null, null, null, null, ct);
        Assert.True(listed.IsSuccess);
        Assert.Equal(1, listed.Value.Total);
        Assert.Equal(txId, listed.Value.Items[0].Id);
        Assert.Equal("Housing", listed.Value.Items[0].CategoryName);

        // --- GET ---
        var gotten = await getHandler.HandleAsync(txId, user.Id, ct);
        Assert.True(gotten.IsSuccess);
        Assert.Equal("Expense", gotten.Value.Type);
        Assert.Equal("Housing", gotten.Value.CategoryName);

        // --- UPDATE ---
        var updated = await updateHandler.HandleAsync(
            txId,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                150m, "Rent updated", TestDate, HousingCategoryId, null),
            user.Id, ct);

        Assert.True(updated.IsSuccess);
        Assert.Equal(150m, updated.Value.Amount);

        // Verify updated fields persisted
        var afterUpdate = await getHandler.HandleAsync(txId, user.Id, ct);
        Assert.Equal("Rent updated", afterUpdate.Value.Description);
        Assert.Equal(150m, afterUpdate.Value.Amount);

        // --- DELETE ---
        var deleted = await deleteHandler.HandleAsync(txId, user.Id, ct);
        Assert.True(deleted.IsSuccess);

        // Transaction no longer appears in list
        var afterDelete = await listHandler.HandleAsync(wallet.Id, user.Id, 1, 50, null, null, null, null, ct);
        Assert.True(afterDelete.IsSuccess);
        Assert.Equal(0, afterDelete.Value.Total);

        // GET returns 404 after delete
        var getAfterDelete = await getHandler.HandleAsync(txId, user.Id, ct);
        Assert.True(getAfterDelete.IsFailure);
        Assert.Equal("not_found", getAfterDelete.Error.Code);
    }

    [Fact]
    public async Task ListTransactions_WithFilters_DateRangeCategoryType()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var user = await CreateUserAsync(db, ct);
        var wallet = await CreateWalletAsync(db, user, ct);
        var recordHandler = new RecordTransactionHandler(db, eventBus, TestClock);
        var listHandler = new ListTransactionsHandler(db);

        // Record 3 transactions on different dates with different types
        await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 50m, "Groceries", "2026-01-10", FoodCategoryId, wallet.Id, null),
            user.Id, ct);

        await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Income", 1000m, "Salary", "2026-01-15", HousingCategoryId, wallet.Id, null),
            user.Id, ct);

        await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 80m, "More food", "2026-02-01", FoodCategoryId, wallet.Id, null),
            user.Id, ct);

        // Filter by date range (January only)
        var janResult = await listHandler.HandleAsync(
            wallet.Id, user.Id, 1, 50, "2026-01-01", "2026-01-31", null, null, ct);
        Assert.True(janResult.IsSuccess);
        Assert.Equal(2, janResult.Value.Total);

        // Filter by category (Food & Dining only)
        var foodResult = await listHandler.HandleAsync(
            wallet.Id, user.Id, 1, 50, null, null, FoodCategoryId, null, ct);
        Assert.True(foodResult.IsSuccess);
        Assert.Equal(2, foodResult.Value.Total);

        // Filter by type (Income only)
        var incomeResult = await listHandler.HandleAsync(
            wallet.Id, user.Id, 1, 50, null, null, null, "Income", ct);
        Assert.True(incomeResult.IsSuccess);
        Assert.Equal(1, incomeResult.Value.Total);
    }

    [Fact]
    public async Task RecordTransaction_NoWalletAccess_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var owner = await CreateUserAsync(db, ct);
        var stranger = await CreateUserAsync(db, ct);
        var wallet = await CreateWalletAsync(db, owner, ct);

        var handler = new RecordTransactionHandler(db, eventBus, TestClock);
        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 100m, "Unauthorized", TestDate, HousingCategoryId, wallet.Id, null),
            stranger.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task RecordTransaction_SystemCategoryAccessible()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var user = await CreateUserAsync(db, ct);
        var wallet = await CreateWalletAsync(db, user, ct);

        var handler = new RecordTransactionHandler(db, eventBus, TestClock);
        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Income", 200m, "Salary", TestDate, HousingCategoryId, wallet.Id, null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(HousingCategoryId, result.Value.CategoryId);
    }

    [Fact]
    public async Task RecordTransaction_OtherUserCategory_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var owner = await CreateUserAsync(db, ct);
        var other = await CreateUserAsync(db, ct);
        var wallet = await CreateWalletAsync(db, owner, ct);

        // Create a custom category belonging to 'other'
        var otherCategory = new Category
        {
            Name = "Other user category",
            UserId = other.Id
        };
        db.Categories.Add(otherCategory);
        await db.SaveChangesAsync(ct);

        var handler = new RecordTransactionHandler(db, eventBus, TestClock);
        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 100m, "Unauthorized category", TestDate, otherCategory.Id, wallet.Id, null),
            owner.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetWallet_AfterTransactions_ReturnsCorrectBalance()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var user = await CreateUserAsync(db, ct);

        // Use CreateWalletHandler so that WalletBalance is created atomically
        var createWalletHandler = new CreateWalletHandler(db, eventBus);
        var createdWallet = await createWalletHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("My Wallet", "Personal"),
            user.Id, ct);

        Assert.True(createdWallet.IsSuccess);
        var walletId = createdWallet.Value.Id;

        var recordHandler = new RecordTransactionHandler(db, eventBus, TestClock);

        // Record income of 1000
        await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Income", 1000m, "Salary", TestDate, HousingCategoryId, walletId, null),
            user.Id, ct);

        // Record expense of 300
        await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 300m, "Rent", TestDate, HousingCategoryId, walletId, null),
            user.Id, ct);

        // GetWallet should return 700
        var getWalletHandler = new GetWalletHandler(db);
        var wallet = await getWalletHandler.HandleAsync(walletId, user.Id, ct);

        Assert.True(wallet.IsSuccess);
        Assert.Equal(700m, wallet.Value.Balance);
    }
}
