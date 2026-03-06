using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.ArchiveWallet;
using Kakeibo.Api.Features.Wallets.CreateWallet;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Wallets.ArchiveWallet;

public sealed class ArchiveWalletHandlerTests
{
    private readonly NodaTime.Testing.FakeClock _clock =
        new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private static async Task<User> CreateTestUserAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        string email = "user@example.com")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "irrelevant-hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task HandleAsync_ValidArchive_SetsDeletedAtAndPublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var user = await CreateTestUserAsync(db);
        var createHandler = new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance);
        var createResult = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("My Wallet", "Personal"), user.Id, ct);

        var handler = new ArchiveWalletHandler(db, eventBus, _clock);
        var result = await handler.HandleAsync(createResult.Value.Id, user.Id, ct);

        var inDb = await db.Wallets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Id == createResult.Value.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.NotNull(inDb),
            () => Assert.Equal(_clock.GetCurrentInstant(), inDb!.DeletedAt));

        eventBus.Received(2).Publish(Arg.Any<WalletCreatedEvent>());
        eventBus.Received(1).Publish(Arg.Is<WalletArchivedEvent>(e =>
            e.WalletId == createResult.Value.Id && e.UserId == user.Id));
    }

    [Fact]
    public async Task HandleAsync_NonOwner_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var owner = await CreateTestUserAsync(db, "owner@example.com");
        var other = await CreateTestUserAsync(db, "other@example.com");

        var createHandler = new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance);
        var createResult = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Owner Wallet", "Personal"), owner.Id, ct);

        var handler = new ArchiveWalletHandler(db, eventBus, _clock);
        var result = await handler.HandleAsync(createResult.Value.Id, other.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_AlreadyArchived_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var user = await CreateTestUserAsync(db);
        var createHandler = new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance);
        var createResult = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("My Wallet", "Personal"), user.Id, ct);

        var handler = new ArchiveWalletHandler(db, eventBus, _clock);
        await handler.HandleAsync(createResult.Value.Id, user.Id, ct);

        // Second archive attempt should return not found
        var result = await handler.HandleAsync(createResult.Value.Id, user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
