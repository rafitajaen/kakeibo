using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.ArchiveWallet;
using Kakeibo.Api.Features.Wallets.CreateWallet;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Wallets.CreateWallet;

public sealed class CreateWalletHandlerTests
{
    private static async Task<User> CreateTestUserAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        string email = "user@example.com",
        string currency = "EUR")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "irrelevant-hash",
            IsVerified = true,
            Currency = currency
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task HandleAsync_ValidPersonalWallet_CreatesWalletAndPublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var handler = new CreateWalletHandler(db, eventBus);

        var user = await CreateTestUserAsync(db);
        var request = new CreateWalletEndpoint.CreateWalletRequest("Checking Account", "Personal");

        var result = await handler.HandleAsync(request, user.Id, ct);

        var inDb = result.IsSuccess
            ? await db.Wallets.FirstOrDefaultAsync(w => w.Id == result.Value.Id, ct)
            : null;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("Checking Account", result.Value.Name),
            () => Assert.Equal("Personal", result.Value.Type),
            () => Assert.Equal("EUR", result.Value.Currency),
            () => Assert.Equal(0m, result.Value.Balance),
            () => Assert.False(result.Value.IsArchived),
            () => Assert.NotNull(inDb),
            () => Assert.Equal(user.Id, inDb!.OwnerId));

        eventBus.Received(1).Publish(Arg.Is<WalletCreatedEvent>(e =>
            e.WalletId == result.Value.Id &&
            e.UserId == user.Id &&
            e.Name == "Checking Account" &&
            e.Type == "Personal"));
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new CreateWalletHandler(db, Substitute.For<IEventBus>());

        var user = await CreateTestUserAsync(db);
        await handler.HandleAsync(new CreateWalletEndpoint.CreateWalletRequest("Savings", "Personal"), user.Id, ct);

        // Second wallet with same name for same user
        var result = await handler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Savings", "Personal"), user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_SameNameDifferentUser_Succeeds()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new CreateWalletHandler(db, Substitute.For<IEventBus>());

        var user1 = await CreateTestUserAsync(db, "alice@example.com");
        var user2 = await CreateTestUserAsync(db, "bob@example.com");

        await handler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Savings", "Personal"), user1.Id, ct);

        var result = await handler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Savings", "Personal"), user2.Id, ct);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new CreateWalletHandler(db, Substitute.For<IEventBus>());

        var unknownUserId = Guid.NewGuid();
        var result = await handler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Wallet", "Personal"), unknownUserId, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_ArchivedWalletWithSameName_AllowsNewWallet()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new NodaTime.Testing.FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();
        var handler = new CreateWalletHandler(db, eventBus);
        var archiveHandler = new ArchiveWalletHandler(db, eventBus, clock);

        var user = await CreateTestUserAsync(db);
        var createResult = await handler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Old Wallet", "Personal"), user.Id, ct);
        await archiveHandler.HandleAsync(createResult.Value.Id, user.Id, ct);

        // Recreating a wallet with the same name after archiving is allowed
        var result = await handler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Old Wallet", "Personal"), user.Id, ct);

        Assert.True(result.IsSuccess);
    }
}
