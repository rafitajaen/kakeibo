using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Features.Transactions.UpdateTransaction;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Transactions.UpdateTransaction;

public sealed class UpdateTransactionHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private const string ValidDate = "2026-02-15";

    // System category IDs from CategoryConfiguration seed data
    private static readonly Guid HousingCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FoodCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003");

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

    // Records a transaction via handler and returns its ID.
    private static async Task<Guid> RecordTransactionAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid userId, Guid walletId,
        string type = "Expense", decimal amount = 100m, string date = ValidDate)
    {
        var eventBus = Substitute.For<IEventBus>();
        var recordHandler = new RecordTransactionHandler(db, eventBus, TestClock);
        var result = await recordHandler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                type, amount, "Test", date, HousingCategoryId, walletId, null),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    [Fact]
    public async Task TransactionNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new UpdateTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            Guid.NewGuid(), // non-existent transaction
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                150m, "Updated", ValidDate, HousingCategoryId, null),
            user.Id, ct);

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
        var txId = await RecordTransactionAsync(db, userA.Id, wallet.Id);
        var handler = new UpdateTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            txId,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                200m, "Hacked", ValidDate, HousingCategoryId, null),
            userB.Id, ct); // userB has no access

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task InvalidDateFormat_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var handler = new UpdateTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            txId,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                100m, "Test", "bad-date", HousingCategoryId, null),
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task DateMoreThanOneYearFuture_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var handler = new UpdateTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        // Clock is 2026-03-01 → more than 1 year ahead = 2027-03-02
        var result = await handler.HandleAsync(
            txId,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                100m, "Test", "2027-03-02", HousingCategoryId, null),
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task CategoryNotAccessible_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        var txId = await RecordTransactionAsync(db, userA.Id, wallet.Id);
        var handler = new UpdateTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        // Category owned by userB — not accessible to userA
        var category = new Category { Name = "Private Category", UserId = userB.Id };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        var result = await handler.HandleAsync(
            txId,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                100m, "Test", ValidDate, category.Id, null),
            userA.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task UpdateDescription_ChangesDescription()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id, "Expense", 100m);
        var handler = new UpdateTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            txId,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                100m, "Updated Description", ValidDate, HousingCategoryId, null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Description", result.Value.Description);
    }

    [Fact]
    public async Task UpdateCategory_ChangesToNewCategory()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        // Start with Housing category, update to Food & Dining
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var handler = new UpdateTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            txId,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                100m, "Test", ValidDate, FoodCategoryId, null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(FoodCategoryId, result.Value.CategoryId);
    }

    [Fact]
    public async Task SharedWalletMember_CanUpdateTransaction()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = userB.Id });
        await db.SaveChangesAsync(ct);
        // userA records the transaction, userB (member) updates it
        var txId = await RecordTransactionAsync(db, userA.Id, wallet.Id, "Expense", 100m);
        var handler = new UpdateTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            txId,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                75m, "Updated by member", ValidDate, HousingCategoryId, null),
            userB.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(75m, result.Value.Amount);
    }

    [Fact]
    public async Task ValidUpdate_PublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new UpdateTransactionHandler(db, eventBus, TestClock);

        var result = await handler.HandleAsync(
            txId,
            new UpdateTransactionEndpoint.UpdateTransactionRequest(
                150m, "Updated", ValidDate, HousingCategoryId, null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        eventBus.Received(1).Publish(Arg.Any<Kakeibo.Api.Features.Transactions.Events.TransactionUpdatedEvent>());
    }
}
