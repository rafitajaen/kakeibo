using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Kakeibo.Api.Features.Goals.UpdateGoal;

namespace Kakeibo.Tests.Features.Goals.UpdateGoal;

public sealed class UpdateGoalHandlerTests
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
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, string name = "Savings", decimal balance = 0m)
    {
        var wallet = new Wallet { Name = name, OwnerId = ownerId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = balance });
        await db.SaveChangesAsync();
        return wallet;
    }

    private static async Task<Guid> CreateGoalAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId,
        Guid walletId,
        string name = "Test Goal",
        decimal target = 1000m)
    {
        var handler = new CreateGoalHandler(db, TestClock);
        var result = await handler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest(name, target, null, walletId),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    [Fact]
    public async Task ValidUpdate_ChangesName_ReturnsSuccess()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, "Old Name");
        var handler = new UpdateGoalHandler(db, TestClock);

        var result = await handler.HandleAsync(
            goalId,
            new UpdateGoalEndpoint.UpdateGoalRequest("New Name", 1500m, null, wallet.Id),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value.Name);
        Assert.Equal(1500m, result.Value.TargetAmount);
    }

    [Fact]
    public async Task WalletChanged_ReseesProgressFromNewWalletBalance()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var walletA = await CreateWalletAsync(db, user.Id, "Wallet A", 500m);
        var walletB = await CreateWalletAsync(db, user.Id, "Wallet B", 2000m);
        var goalId = await CreateGoalAsync(db, user.Id, walletA.Id, "Goal");
        var handler = new UpdateGoalHandler(db, TestClock);

        // Change wallet to B — CurrentProgress should reset to 2000
        var result = await handler.HandleAsync(
            goalId,
            new UpdateGoalEndpoint.UpdateGoalRequest("Goal", 5000m, null, walletB.Id),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(2000m, result.Value.CurrentProgress);
        Assert.Equal("Wallet B", result.Value.WalletName);
    }

    [Fact]
    public async Task GoalNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new UpdateGoalHandler(db, TestClock);

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            new UpdateGoalEndpoint.UpdateGoalRequest("Name", 1000m, null, wallet.Id),
            user.Id, ct);

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
        var handler = new UpdateGoalHandler(db, TestClock);

        var result = await handler.HandleAsync(
            goalId,
            new UpdateGoalEndpoint.UpdateGoalRequest("Hijacked", 999m, null, wallet.Id),
            userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task InvalidDeadline_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id);
        var handler = new UpdateGoalHandler(db, TestClock);

        var result = await handler.HandleAsync(
            goalId,
            new UpdateGoalEndpoint.UpdateGoalRequest("Name", 1000m, "bad-date", wallet.Id),
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task TargetAmountChange_DoesNotResetLastMilestone()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, target: 1000m);

        // Manually set LastMilestone to 50
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.LastMilestone = 50;
        await db.SaveChangesAsync(ct);

        var handler = new UpdateGoalHandler(db, TestClock);

        // Change TargetAmount only
        var result = await handler.HandleAsync(
            goalId,
            new UpdateGoalEndpoint.UpdateGoalRequest("Goal", 2000m, null, wallet.Id),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        // LastMilestone should remain at 50 (not reset)
        Assert.Equal(50, result.Value.LastMilestone);
    }
}
