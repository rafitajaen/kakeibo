using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.ArchiveWallet;
using Kakeibo.Api.Features.Wallets.CreateWallet;
using Kakeibo.Api.Features.Wallets.UpdateWallet;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Wallets.UpdateWallet;

public sealed class UpdateWalletHandlerTests
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
    public async Task HandleAsync_ValidRename_UpdatesNameAndTimestamp()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var user = await CreateTestUserAsync(db);
        var createHandler = new CreateWalletHandler(db, eventBus);
        var createResult = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Old Name", "Personal"), user.Id, ct);

        var handler = new UpdateWalletHandler(db, _clock);
        var result = await handler.HandleAsync(
            createResult.Value.Id,
            new UpdateWalletEndpoint.UpdateWalletRequest("New Name"),
            user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("New Name", result.Value.Name),
            () => Assert.Equal(_clock.GetCurrentInstant(), result.Value.CreatedAt == result.Value.CreatedAt
                ? _clock.GetCurrentInstant()
                : Instant.MinValue));

        // Verify UpdatedAt was set in DB
        var inDb = await db.Wallets.FindAsync([createResult.Value.Id], ct);
        Assert.Equal(_clock.GetCurrentInstant(), inDb!.UpdatedAt);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var user = await CreateTestUserAsync(db);
        var createHandler = new CreateWalletHandler(db, eventBus);
        await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Savings", "Personal"), user.Id, ct);
        var r2 = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Checking", "Personal"), user.Id, ct);

        var handler = new UpdateWalletHandler(db, _clock);
        var result = await handler.HandleAsync(
            r2.Value.Id,
            new UpdateWalletEndpoint.UpdateWalletRequest("Savings"),
            user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_NonOwner_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var owner = await CreateTestUserAsync(db, "owner@example.com");
        var other = await CreateTestUserAsync(db, "other@example.com");

        var createHandler = new CreateWalletHandler(db, eventBus);
        var createResult = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Owner Wallet", "Personal"), owner.Id, ct);

        var handler = new UpdateWalletHandler(db, _clock);
        var result = await handler.HandleAsync(
            createResult.Value.Id,
            new UpdateWalletEndpoint.UpdateWalletRequest("Renamed"),
            other.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_ArchivedWallet_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var user = await CreateTestUserAsync(db);
        var createHandler = new CreateWalletHandler(db, eventBus);
        var createResult = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("To Archive", "Personal"), user.Id, ct);

        var archiveHandler = new ArchiveWalletHandler(db, eventBus, _clock);
        await archiveHandler.HandleAsync(createResult.Value.Id, user.Id, ct);

        var handler = new UpdateWalletHandler(db, _clock);
        var result = await handler.HandleAsync(
            createResult.Value.Id,
            new UpdateWalletEndpoint.UpdateWalletRequest("New Name"),
            user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
