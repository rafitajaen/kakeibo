using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.ArchiveWallet;
using Kakeibo.Api.Features.Wallets.CreateWallet;
using Kakeibo.Api.Features.Wallets.GetWallet;
using Kakeibo.Api.Features.Wallets.ListWallets;
using Kakeibo.Api.Features.Wallets.UpdateWallet;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Integration.Wallets;

/// <summary>
/// Integration tests for the personal wallet CRUD lifecycle.
/// Chains multiple handlers together against a real PostgreSQL database to verify
/// the full Phase 2a acceptance criteria: create → list → get → update → archive.
/// </summary>
public sealed class WalletCrudIntegrationTests
{
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

    [Fact]
    public async Task PersonalWallet_FullCrudLifecycle_CreateListGetUpdateArchive()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();
        var user = await CreateUserAsync(db);

        // --- CREATE ---
        var createHandler = new CreateWalletHandler(db, eventBus);
        var createResult = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Checking Account", "Personal"),
            user.Id, ct);

        Assert.True(createResult.IsSuccess);
        var walletId = createResult.Value.Id;
        Assert.Equal("Checking Account", createResult.Value.Name);
        Assert.Equal("Personal", createResult.Value.Type);
        Assert.False(createResult.Value.IsArchived);

        // --- LIST (appears in active list) ---
        var listHandler = new ListWalletsHandler(db);
        var listResult = await listHandler.HandleAsync(user.Id, includeArchived: false, ct);

        Assert.Single(listResult);
        Assert.Equal(walletId, listResult[0].Id);

        // --- GET ---
        var getHandler = new GetWalletHandler(db);
        var getResult = await getHandler.HandleAsync(walletId, user.Id, ct);

        Assert.True(getResult.IsSuccess);
        Assert.Equal("Checking Account", getResult.Value.Name);
        Assert.False(getResult.Value.IsArchived);

        // --- UPDATE ---
        var updateHandler = new UpdateWalletHandler(db, clock);
        var updateResult = await updateHandler.HandleAsync(
            walletId,
            new UpdateWalletEndpoint.UpdateWalletRequest("Main Checking"),
            user.Id, ct);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Main Checking", updateResult.Value.Name);

        // Verify name change persisted
        var afterUpdate = await getHandler.HandleAsync(walletId, user.Id, ct);
        Assert.Equal("Main Checking", afterUpdate.Value.Name);

        // --- ARCHIVE ---
        var archiveHandler = new ArchiveWalletHandler(db, eventBus, clock);
        var archiveResult = await archiveHandler.HandleAsync(walletId, user.Id, ct);

        Assert.True(archiveResult.IsSuccess);

        // GET after archive returns 404 (archived == not found per BUG-001 fix)
        var afterArchive = await getHandler.HandleAsync(walletId, user.Id, ct);
        Assert.True(afterArchive.IsFailure);
        Assert.Equal("not_found", afterArchive.Error.Code);

        // LIST without includeArchived does not show the wallet
        var listAfterArchive = await listHandler.HandleAsync(user.Id, includeArchived: false, ct);
        Assert.Empty(listAfterArchive);

        // LIST with includeArchived does show it
        var listWithArchived = await listHandler.HandleAsync(user.Id, includeArchived: true, ct);
        Assert.Single(listWithArchived);
        Assert.True(listWithArchived[0].IsArchived);
    }

    [Fact]
    public async Task CreateWallet_DuplicateName_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var user = await CreateUserAsync(db);
        var handler = new CreateWalletHandler(db, eventBus);

        await handler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Savings", "Personal"), user.Id, ct);

        var duplicate = await handler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Savings", "Personal"), user.Id, ct);

        Assert.True(duplicate.IsFailure);
        Assert.Equal("conflict", duplicate.Error.Code);
    }

    [Fact]
    public async Task UpdateWallet_ArchivedWallet_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();
        var user = await CreateUserAsync(db);

        var created = await new CreateWalletHandler(db, eventBus).HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Temp", "Personal"), user.Id, ct);

        await new ArchiveWalletHandler(db, eventBus, clock).HandleAsync(created.Value.Id, user.Id, ct);

        var updateResult = await new UpdateWalletHandler(db, clock).HandleAsync(
            created.Value.Id,
            new UpdateWalletEndpoint.UpdateWalletRequest("New Name"),
            user.Id, ct);

        Assert.True(updateResult.IsFailure);
        Assert.Equal("not_found", updateResult.Error.Code);
    }

    [Fact]
    public async Task GetWallet_ArchivedWallet_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();
        var user = await CreateUserAsync(db);

        var created = await new CreateWalletHandler(db, eventBus).HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Temp", "Personal"), user.Id, ct);

        await new ArchiveWalletHandler(db, eventBus, clock).HandleAsync(created.Value.Id, user.Id, ct);

        var getResult = await new GetWalletHandler(db).HandleAsync(created.Value.Id, user.Id, ct);

        Assert.True(getResult.IsFailure);
        Assert.Equal("not_found", getResult.Error.Code);
    }
}
