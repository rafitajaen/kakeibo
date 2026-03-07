using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Transactions.RecordTransaction;

public sealed class RecordTransactionHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    // A valid past date within the allowed range
    private const string ValidDate = "2026-02-15";

    // System category IDs from CategoryConfiguration seed data
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

    [Fact]
    public async Task InvalidTransactionType_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "InvalidType", 100m, "Test", ValidDate, HousingCategoryId, wallet.Id, null),
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task InvalidDateFormat_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 100m, "Test", "not-a-date", HousingCategoryId, wallet.Id, null),
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
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        // Clock is 2026-03-01 → more than 1 year ahead = 2027-03-02
        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 100m, "Test", "2027-03-02", HousingCategoryId, wallet.Id, null),
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task WalletNotAccessible_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        // wallet owned by userA
        var wallet = await CreateWalletAsync(db, userA.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 100m, "Test", ValidDate, HousingCategoryId, wallet.Id, null),
            userB.Id, ct); // userB has no access

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task CategoryNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);
        var nonExistentCategoryId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 100m, "Test", ValidDate, nonExistentCategoryId, wallet.Id, null),
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task OtherUsersCategory_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        // Create a custom category owned by userB
        var category = new Category { Name = "UserB Category", UserId = userB.Id };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        // userA tries to record a transaction with userB's category
        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 100m, "Test", ValidDate, category.Id, wallet.Id, null),
            userA.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task Transfer_WithoutDestinationWallet_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Transfer", 100m, "Test", ValidDate, HousingCategoryId, wallet.Id,
                DestinationWalletId: null), // no destination
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Transfer_SameSourceAndDestination_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Transfer", 100m, "Test", ValidDate, HousingCategoryId,
                wallet.Id, wallet.Id), // same source and destination
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Transfer_DestinationWalletNotAccessible_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var sourceWallet = await CreateWalletAsync(db, userA.Id);
        // destination wallet owned by userB — userA has no access
        var destWallet = await CreateWalletAsync(db, userB.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Transfer", 100m, "Test", ValidDate, HousingCategoryId,
                sourceWallet.Id, destWallet.Id),
            userA.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task SharedWalletMember_CanRecordTransaction()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        // Add userB as a wallet member
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = userB.Id });
        await db.SaveChangesAsync(ct);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 50m, "Shared expense", ValidDate, HousingCategoryId, wallet.Id, null),
            userB.Id, ct); // member, not owner

        Assert.True(result.IsSuccess);
        Assert.Equal("Expense", result.Value.Type);
        Assert.Equal(50m, result.Value.Amount);
        Assert.Equal(userB.Id, result.Value.UserId);
    }

    [Fact]
    public async Task ValidIncome_PublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new RecordTransactionHandler(db, eventBus, TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Income", 200m, "Salary", ValidDate, HousingCategoryId, wallet.Id, null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        eventBus.Received(1).Publish(Arg.Any<Kakeibo.Api.Features.Transactions.Events.TransactionRecordedEvent>());
    }

    [Fact]
    public async Task WithNotes_PersistsAndReturnsNotes()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 50m, "Lunch", ValidDate, HousingCategoryId, wallet.Id, null,
                Notes: "Expense with a note"),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Expense with a note", result.Value.Notes);
    }

    [Fact]
    public async Task WithoutNotes_ReturnsNullNotes()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);

        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Income", 100m, "Salary", ValidDate, HousingCategoryId, wallet.Id, null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Notes);
    }
}
