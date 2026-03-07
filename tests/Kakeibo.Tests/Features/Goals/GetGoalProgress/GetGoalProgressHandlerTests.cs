using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Kakeibo.Api.Features.Goals.GetGoalProgress;

namespace Kakeibo.Tests.Features.Goals.GetGoalProgress;

public sealed class GetGoalProgressHandlerTests
{
    // Fixed clock: 2026-03-01
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

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

    private static async Task<(Wallet wallet, WalletBalance balance)> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        var walletBalance = new WalletBalance { WalletId = wallet.Id, Balance = 0m };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(walletBalance);
        await db.SaveChangesAsync();
        return (wallet, walletBalance);
    }

    private static async Task<Guid> CreateGoalAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId,
        Guid walletId,
        decimal targetAmount,
        string? deadline = null)
    {
        var handler = new CreateGoalHandler(db, TestClock);
        var result = await handler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest("Vacation Fund", targetAmount, deadline, walletId),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    [Fact]
    public async Task ReturnsNotStarted_WhenProgressIsZero()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);
        var handler = new GetGoalProgressHandler(db, TestClock);

        var result = await handler.HandleAsync(goalId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("NotStarted", result.Value.Status);
        Assert.Equal(0m, result.Value.CurrentProgress);
        Assert.Equal(1000m, result.Value.Remaining);
        Assert.Equal(0m, result.Value.PercentageComplete);
    }

    [Fact]
    public async Task ReturnsInProgress_WhenProgressIsBelow75Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Set 40% progress
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.CurrentProgress = 400m;
        await db.SaveChangesAsync(ct);

        var handler = new GetGoalProgressHandler(db, TestClock);

        var result = await handler.HandleAsync(goalId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("InProgress", result.Value.Status);
        Assert.Equal(400m, result.Value.CurrentProgress);
        Assert.Equal(600m, result.Value.Remaining);
        Assert.Equal(40m, result.Value.PercentageComplete);
    }

    [Fact]
    public async Task ReturnsNearTarget_WhenProgressIsAtOrAbove75Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Set exactly 75%
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.CurrentProgress = 750m;
        await db.SaveChangesAsync(ct);

        var handler = new GetGoalProgressHandler(db, TestClock);

        var result = await handler.HandleAsync(goalId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("NearTarget", result.Value.Status);
        Assert.Equal(750m, result.Value.CurrentProgress);
        Assert.Equal(250m, result.Value.Remaining);
        Assert.Equal(75m, result.Value.PercentageComplete);
    }

    [Fact]
    public async Task ReturnsAchieved_WhenProgressMeetsOrExceedsTarget()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Set to exactly target
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.CurrentProgress = 1000m;
        await db.SaveChangesAsync(ct);

        var handler = new GetGoalProgressHandler(db, TestClock);

        var result = await handler.HandleAsync(goalId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Achieved", result.Value.Status);
        Assert.Equal(0m, result.Value.Remaining);
        Assert.Equal(100m, result.Value.PercentageComplete);
    }

    [Fact]
    public async Task CalculatesDaysRemaining_WhenDeadlineIsSet()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);

        // Clock is 2026-03-01; deadline is 2026-03-11 → 10 days remaining
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m, "2026-03-11");
        var handler = new GetGoalProgressHandler(db, TestClock);

        var result = await handler.HandleAsync(goalId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.DaysRemaining);
        Assert.Equal("2026-03-11", result.Value.Deadline);
    }

    [Fact]
    public async Task ReturnsNullDaysRemaining_WhenNoDeadline()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);
        var handler = new GetGoalProgressHandler(db, TestClock);

        var result = await handler.HandleAsync(goalId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.DaysRemaining);
        Assert.Null(result.Value.Deadline);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenGoalDoesNotExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new GetGoalProgressHandler(db, TestClock);

        var result = await handler.HandleAsync(Guid.NewGuid(), user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenDifferentUser()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, userA.Id);
        var goalId = await CreateGoalAsync(db, userA.Id, wallet.Id, 1000m);
        var handler = new GetGoalProgressHandler(db, TestClock);

        var result = await handler.HandleAsync(goalId, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }
}
