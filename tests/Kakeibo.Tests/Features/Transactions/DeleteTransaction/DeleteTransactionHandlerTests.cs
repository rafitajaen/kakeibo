using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.DeleteTransaction;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Transactions.DeleteTransaction;

public sealed class DeleteTransactionHandlerTests
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
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = initialBalance });
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = ownerId, Role = WalletMemberRole.Owner });
        await db.SaveChangesAsync();
        return wallet;
    }

    // Records a transaction and returns its ID.
    private static async Task<Guid> RecordAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid userId, Guid walletId,
        string type = "Expense", decimal amount = 100m, Guid? destWalletId = null)
    {
        var recordHandler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);
        var result = await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                type, amount, "Test", ValidDate, HousingCategoryId, walletId, destWalletId),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    [Fact]
    public async Task TransactionNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new DeleteTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

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
        var handler = new DeleteTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(txId, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task AlreadyDeleted_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordAsync(db, user.Id, wallet.Id);
        var handler = new DeleteTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        // First delete — succeeds
        await handler.HandleAsync(txId, user.Id, ct);

        // Second delete — transaction is already soft-deleted, returns not_found
        var result = await handler.HandleAsync(txId, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task SetsDeletedAt_OnSoftDelete()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordAsync(db, user.Id, wallet.Id);
        var handler = new DeleteTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(txId, user.Id, ct);

        Assert.True(result.IsSuccess);

        // Verify the transaction has DeletedAt set in the database
        var deleted = await db.Transactions.FindAsync(new object[] { txId }, ct);
        Assert.NotNull(deleted);
        Assert.NotNull(deleted.DeletedAt);
        Assert.Equal(TestClock.GetCurrentInstant(), deleted.DeletedAt.Value);
    }

    [Fact]
    public async Task SharedWalletMember_CanDeleteTransaction()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = userB.Id });
        await db.SaveChangesAsync(ct);
        var txId = await RecordAsync(db, userA.Id, wallet.Id);
        var handler = new DeleteTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        // userB (member) can delete the transaction
        var result = await handler.HandleAsync(txId, userB.Id, ct);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidDelete_PublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordAsync(db, user.Id, wallet.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new DeleteTransactionHandler(db, eventBus, TestClock);

        var result = await handler.HandleAsync(txId, user.Id, ct);

        Assert.True(result.IsSuccess);
        eventBus.Received(1).Publish(Arg.Any<Kakeibo.Api.Features.Transactions.Events.TransactionDeletedEvent>());
    }
}
