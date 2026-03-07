using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.ListTransactions;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Transactions.ListTransactions;

public sealed class ListTransactionsHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private static readonly Guid HousingCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FoodCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003");

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
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, decimal initialBalance = 1000m)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = initialBalance });
        await db.SaveChangesAsync();
        return wallet;
    }

    // Records a transaction with specified date and type.
    private static async Task RecordAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid userId, Guid walletId,
        string type = "Expense", decimal amount = 100m, string date = "2026-02-15",
        Guid? categoryId = null)
    {
        var recordHandler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);
        await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                type, amount, "Test", date, categoryId ?? HousingCategoryId, walletId, null),
            userId, CancellationToken.None);
    }

    [Fact]
    public async Task WalletNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new ListTransactionsHandler(db);

        var result = await handler.HandleAsync(
            Guid.NewGuid(), user.Id, 1, 50, null, null, null, null, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task WalletNotAccessible_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        var handler = new ListTransactionsHandler(db);

        var result = await handler.HandleAsync(
            wallet.Id, userB.Id, 1, 50, null, null, null, null, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task EmptyWallet_ReturnsEmptyList()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new ListTransactionsHandler(db);

        var result = await handler.HandleAsync(
            wallet.Id, user.Id, 1, 50, null, null, null, null, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Total);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task MultipleTransactions_ReturnedOrderedByDateDescending()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new ListTransactionsHandler(db);

        await RecordAsync(db, user.Id, wallet.Id, date: "2026-01-10", amount: 100m);
        await RecordAsync(db, user.Id, wallet.Id, date: "2026-02-20", amount: 200m);
        await RecordAsync(db, user.Id, wallet.Id, date: "2026-01-25", amount: 150m);

        var result = await handler.HandleAsync(
            wallet.Id, user.Id, 1, 50, null, null, null, null, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Total);
        // Most recent first
        Assert.Equal(200m, result.Value.Items[0].Amount); // 2026-02-20
        Assert.Equal(150m, result.Value.Items[1].Amount); // 2026-01-25
        Assert.Equal(100m, result.Value.Items[2].Amount); // 2026-01-10
    }

    [Fact]
    public async Task FilterByType_OnlyReturnsMatchingType()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id, initialBalance: 2000m);
        var handler = new ListTransactionsHandler(db);

        await RecordAsync(db, user.Id, wallet.Id, type: "Expense", amount: 100m);
        await RecordAsync(db, user.Id, wallet.Id, type: "Income", amount: 500m);
        await RecordAsync(db, user.Id, wallet.Id, type: "Expense", amount: 200m);

        var result = await handler.HandleAsync(
            wallet.Id, user.Id, 1, 50, null, null, null, "Expense", ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Total);
        Assert.All(result.Value.Items, item => Assert.Equal("Expense", item.Type));
    }

    [Fact]
    public async Task FilterByCategory_OnlyReturnsMatchingCategory()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id, initialBalance: 2000m);
        var handler = new ListTransactionsHandler(db);

        await RecordAsync(db, user.Id, wallet.Id, categoryId: HousingCategoryId);
        await RecordAsync(db, user.Id, wallet.Id, categoryId: FoodCategoryId);
        await RecordAsync(db, user.Id, wallet.Id, categoryId: HousingCategoryId);

        var result = await handler.HandleAsync(
            wallet.Id, user.Id, 1, 50, null, null, HousingCategoryId, null, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Total);
        Assert.All(result.Value.Items, item => Assert.Equal(HousingCategoryId, item.CategoryId));
    }

    [Fact]
    public async Task FilterByDateRange_OnlyReturnsMatchingTransactions()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id, initialBalance: 2000m);
        var handler = new ListTransactionsHandler(db);

        await RecordAsync(db, user.Id, wallet.Id, date: "2026-01-05");
        await RecordAsync(db, user.Id, wallet.Id, date: "2026-01-15");
        await RecordAsync(db, user.Id, wallet.Id, date: "2026-02-10");

        // Filter from 2026-01-10 to 2026-01-31
        var result = await handler.HandleAsync(
            wallet.Id, user.Id, 1, 50, "2026-01-10", "2026-01-31", null, null, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total); // only 2026-01-15 matches
        Assert.Equal("2026-01-15", result.Value.Items[0].Date);
    }

    [Fact]
    public async Task Pagination_SecondPage_ReturnsCorrectItems()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id, initialBalance: 5000m);
        var handler = new ListTransactionsHandler(db);

        // Record 5 transactions on different dates
        for (var i = 1; i <= 5; i++)
        {
            await RecordAsync(db, user.Id, wallet.Id, amount: i * 10m, date: $"2026-01-{i:D2}");
        }

        // Page 1 with pageSize=3 → first 3 items (most recent first)
        var page1 = await handler.HandleAsync(
            wallet.Id, user.Id, 1, 3, null, null, null, null, ct);

        Assert.True(page1.IsSuccess);
        Assert.Equal(5, page1.Value.Total);
        Assert.Equal(3, page1.Value.Items.Count);

        // Page 2 with pageSize=3 → remaining 2 items
        var page2 = await handler.HandleAsync(
            wallet.Id, user.Id, 2, 3, null, null, null, null, ct);

        Assert.True(page2.IsSuccess);
        Assert.Equal(5, page2.Value.Total);
        Assert.Equal(2, page2.Value.Items.Count);
    }

    [Fact]
    public async Task SharedWalletMember_CanListTransactions()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = userB.Id });
        await db.SaveChangesAsync(ct);
        await RecordAsync(db, userA.Id, wallet.Id, amount: 300m);
        var handler = new ListTransactionsHandler(db);

        // userB (member) can list transactions in the shared wallet
        var result = await handler.HandleAsync(
            wallet.Id, userB.Id, 1, 50, null, null, null, null, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
        Assert.Equal(300m, result.Value.Items[0].Amount);
    }
}
