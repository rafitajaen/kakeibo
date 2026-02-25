using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.GetTransaction;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Transactions.GetTransaction;

public sealed class GetTransactionHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private const string ValidDate = "2026-02-15";
    private static readonly Guid HousingCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");

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
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, decimal initialBalance = 1000m)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = initialBalance });
        await db.SaveChangesAsync();
        return wallet;
    }

    // Records a transaction and returns its ID.
    private static async Task<Guid> RecordAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid userId, Guid walletId,
        string type = "Expense", decimal amount = 100m)
    {
        var recordHandler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);
        var result = await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                type, amount, "Test description", ValidDate, HousingCategoryId, walletId, null),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    [Fact]
    public async Task TransactionNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new GetTransactionHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task OtherUsersTransaction_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        var txId = await RecordAsync(db, userA.Id, wallet.Id);
        var handler = new GetTransactionHandler(db);

        var result = await handler.HandleAsync(txId, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task ValidRequest_ReturnsTransactionWithCategoryName()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordAsync(db, user.Id, wallet.Id, "Expense", 250m);
        var handler = new GetTransactionHandler(db);

        var result = await handler.HandleAsync(txId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(txId, result.Value.Id);
        Assert.Equal("Expense", result.Value.Type);
        Assert.Equal(250m, result.Value.Amount);
        Assert.Equal("Test description", result.Value.Description);
        Assert.Equal(ValidDate, result.Value.Date);
        Assert.Equal("Housing", result.Value.CategoryName); // system category name from seed
        Assert.Equal(wallet.Id, result.Value.WalletId);
    }

    [Fact]
    public async Task SharedWalletMember_CanGetTransaction()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = userB.Id });
        await db.SaveChangesAsync(ct);
        var txId = await RecordAsync(db, userA.Id, wallet.Id);
        var handler = new GetTransactionHandler(db);

        // userB (member) can get the transaction
        var result = await handler.HandleAsync(txId, userB.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(txId, result.Value.Id);
    }

    [Fact]
    public async Task DateFormattedAsIso_InResponse()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordAsync(db, user.Id, wallet.Id);
        var handler = new GetTransactionHandler(db);

        var result = await handler.HandleAsync(txId, user.Id, ct);

        Assert.True(result.IsSuccess);
        // Date must be returned as ISO 8601 (YYYY-MM-DD)
        Assert.Equal(ValidDate, result.Value.Date);
    }
}
