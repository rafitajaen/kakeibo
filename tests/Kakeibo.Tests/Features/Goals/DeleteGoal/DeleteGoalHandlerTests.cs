using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Kakeibo.Api.Features.Goals.DeleteGoal;
using Kakeibo.Api.Features.Goals.ListGoals;
using NodaTime;
using NodaTime.Testing;

namespace Kakeibo.Tests.Features.Goals.DeleteGoal;

public sealed class DeleteGoalHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

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
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });
        await db.SaveChangesAsync();
        return wallet;
    }

    private static async Task<Guid> CreateGoalAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid userId, Guid walletId)
    {
        var handler = new CreateGoalHandler(db, TestClock);
        var result = await handler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest("Test Goal", 1000m, null, walletId),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    [Fact]
    public async Task SoftDeletesGoal_RemovesFromListing()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id);
        var deleteHandler = new DeleteGoalHandler(db, TestClock);
        var listHandler = new ListGoalsHandler(db);

        var result = await deleteHandler.HandleAsync(goalId, user.Id, ct);

        Assert.True(result.IsSuccess);

        // Soft-deleted goal should not appear in list
        var goals = await listHandler.HandleAsync(user.Id, null, ct);
        Assert.Empty(goals);
    }

    [Fact]
    public async Task GoalNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new DeleteGoalHandler(db, TestClock);

        var result = await handler.HandleAsync(Guid.NewGuid(), user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task OtherUsersGoal_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        var goalId = await CreateGoalAsync(db, userA.Id, wallet.Id);
        var handler = new DeleteGoalHandler(db, TestClock);

        var result = await handler.HandleAsync(goalId, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task SetsDeletedAt_Idempotent_AlreadyDeleted_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id);
        var handler = new DeleteGoalHandler(db, TestClock);

        // First delete
        await handler.HandleAsync(goalId, user.Id, ct);

        // Second delete — already soft-deleted, should return not_found
        var result = await handler.HandleAsync(goalId, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }
}
