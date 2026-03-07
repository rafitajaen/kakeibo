using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.ArchiveWallet;
using Kakeibo.Api.Features.Wallets.CreateWallet;
using Kakeibo.Api.Features.Wallets.ListWallets;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Wallets.ListWallets;

public sealed class ListWalletsHandlerTests
{
    private static async Task<User> CreateTestUserAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        string email = "user@example.com")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "irrelevant-hash",
            Username = $"user_{Guid.NewGuid():N}"[..12],
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task HandleAsync_NoWallets_ReturnsEmptyList()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateTestUserAsync(db);
        var handler = new ListWalletsHandler(db);

        var result = await handler.HandleAsync(user.Id, includeArchived: false, ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_MultipleWallets_ReturnsOnlyCallerWallets()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var user1 = await CreateTestUserAsync(db, "alice@example.com");
        var user2 = await CreateTestUserAsync(db, "bob@example.com");

        var createHandler = new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance);
        await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Alice Wallet 1", "Personal"), user1.Id, ct);
        await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Alice Wallet 2", "Personal"), user1.Id, ct);
        await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Bob Wallet", "Personal"), user2.Id, ct);

        var handler = new ListWalletsHandler(db);
        var result = await handler.HandleAsync(user1.Id, includeArchived: false, ct);

        Assert.Multiple(
            () => Assert.Equal(2, result.Count),
            () => Assert.All(result, w => Assert.Contains("Alice", w.Name)));
    }

    [Fact]
    public async Task HandleAsync_ArchivedExcludedByDefault()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new NodaTime.Testing.FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();

        var user = await CreateTestUserAsync(db);
        var createHandler = new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance);
        var archiveHandler = new ArchiveWalletHandler(db, eventBus, clock);

        var r1 = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Active", "Personal"), user.Id, ct);
        var r2 = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Archived", "Personal"), user.Id, ct);
        await archiveHandler.HandleAsync(r2.Value.Id, user.Id, ct);

        var handler = new ListWalletsHandler(db);
        var result = await handler.HandleAsync(user.Id, includeArchived: false, ct);

        Assert.Multiple(
            () => Assert.Single(result),
            () => Assert.Equal("Active", result[0].Name));
    }

    [Fact]
    public async Task HandleAsync_IncludeArchived_ReturnsAllWallets()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new NodaTime.Testing.FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();

        var user = await CreateTestUserAsync(db);
        var createHandler = new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance);
        var archiveHandler = new ArchiveWalletHandler(db, eventBus, clock);

        var r1 = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Active", "Personal"), user.Id, ct);
        var r2 = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Archived", "Personal"), user.Id, ct);
        await archiveHandler.HandleAsync(r2.Value.Id, user.Id, ct);

        var handler = new ListWalletsHandler(db);
        var result = await handler.HandleAsync(user.Id, includeArchived: true, ct);

        Assert.Multiple(
            () => Assert.Equal(2, result.Count),
            () => Assert.Contains(result, w => w.IsArchived),
            () => Assert.Contains(result, w => !w.IsArchived));
    }
}
