using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Kakeibo.Api.Features.Goals.Events;
using Kakeibo.Api.Features.Goals.GetGoalProgress;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Integration.Goals;

/// <summary>
/// Integration tests for goal progress tracking across the full lifecycle:
/// transaction recorded → progress updated → milestones detected → progress endpoint.
/// </summary>
public sealed class GoalProgressTrackingTests
{
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

    private static async Task<(Wallet wallet, WalletBalance walletBalance)> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        var walletBalance = new WalletBalance { WalletId = wallet.Id, Balance = 0m };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(walletBalance);
        await db.SaveChangesAsync();
        return (wallet, walletBalance);
    }

    [Fact]
    public async Task FullProgressLifecycle_RecordUpdateDelete()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, walletBalance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();

        // Create goal: target 1000
        var createHandler = new CreateGoalHandler(db, TestClock);
        var createResult = await createHandler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest("Europe Vacation", 1000m, null, wallet.Id),
            user.Id, ct);
        Assert.True(createResult.IsSuccess);
        var goalId = createResult.Value.Id;

        var recordedHandler = new TransactionRecordedGoalHandler(db, eventBus);
        var updatedHandler = new TransactionUpdatedGoalHandler(db, eventBus);
        var deletedHandler = new TransactionDeletedGoalHandler(db, eventBus);
        var progressHandler = new GetGoalProgressHandler(db, TestClock);

        // Step 1: Record income — balance becomes 300 (30% of target)
        walletBalance.Balance = 300m;
        await db.SaveChangesAsync(ct);

        await recordedHandler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 300m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        var progress = await progressHandler.HandleAsync(goalId, user.Id, ct);
        Assert.True(progress.IsSuccess);
        Assert.Equal(300m, progress.Value.CurrentProgress);
        Assert.Equal("InProgress", progress.Value.Status);
        Assert.Equal(25, (await db.Goals.FindAsync([goalId], ct))!.LastMilestone);
        eventBus.Received(1).Publish(Arg.Is<GoalMilestoneReachedEvent>(e => e.MilestonePercent == 25));

        // Step 2: Update transaction — balance is now 800 (80%, crosses 50% and 75%)
        walletBalance.Balance = 800m;
        await db.SaveChangesAsync(ct);

        await updatedHandler.HandleAsync(new TransactionUpdatedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 800m,
            OldAmount = 300m,
            CategoryId = Guid.NewGuid(),
            OldCategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
            OldDate = new LocalDate(2026, 3, 1),
        }, ct);

        progress = await progressHandler.HandleAsync(goalId, user.Id, ct);
        Assert.Equal(800m, progress.Value.CurrentProgress);
        Assert.Equal("NearTarget", progress.Value.Status);
        Assert.Equal(75, (await db.Goals.FindAsync([goalId], ct))!.LastMilestone);

        // Step 3: Delete transaction — balance drops to 200 (20%, below all milestones but LastMilestone stays)
        walletBalance.Balance = 200m;
        await db.SaveChangesAsync(ct);

        await deletedHandler.HandleAsync(new TransactionDeletedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 600m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        progress = await progressHandler.HandleAsync(goalId, user.Id, ct);
        Assert.Equal(200m, progress.Value.CurrentProgress);
        Assert.Equal("InProgress", progress.Value.Status);

        // LastMilestone stays at 75 — does not reset on progress drop
        var finalGoal = await db.Goals.FindAsync([goalId], ct);
        Assert.Equal(75, finalGoal!.LastMilestone);
    }

    [Fact]
    public async Task GoalAchieved_WhenProgressReachesTarget()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, walletBalance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();

        var createHandler = new CreateGoalHandler(db, TestClock);
        var createResult = await createHandler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest("New Car", 5000m, null, wallet.Id),
            user.Id, ct);
        var goalId = createResult.Value.Id;

        // Wallet balance jumps to 5000 (achieved in one shot)
        walletBalance.Balance = 5000m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 5000m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        var goal = await db.Goals.FindAsync([goalId], ct);
        Assert.Equal(5000m, goal!.CurrentProgress);
        Assert.Equal(100, goal.LastMilestone);

        // GoalAchievedEvent published once
        eventBus.Received(1).Publish(Arg.Any<GoalAchievedEvent>());

        var progressHandler = new GetGoalProgressHandler(db, TestClock);
        var progress = await progressHandler.HandleAsync(goalId, user.Id, ct);
        Assert.Equal("Achieved", progress.Value.Status);
        Assert.Equal(0m, progress.Value.Remaining);
    }

    [Fact]
    public async Task MilestoneNotRepublished_WhenDropAndRecross()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, walletBalance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();

        var createHandler = new CreateGoalHandler(db, TestClock);
        var createResult = await createHandler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest("Goal", 1000m, null, wallet.Id),
            user.Id, ct);
        var goalId = createResult.Value.Id;

        var recordedHandler = new TransactionRecordedGoalHandler(db, eventBus);

        // First: balance reaches 300 → crosses 25%
        walletBalance.Balance = 300m;
        await db.SaveChangesAsync(ct);
        await recordedHandler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 300m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        // Milestone 25% published once
        eventBus.Received(1).Publish(Arg.Is<GoalMilestoneReachedEvent>(e => e.MilestonePercent == 25));

        // Second: balance drops below 25% and then re-crosses it
        walletBalance.Balance = 200m;
        await db.SaveChangesAsync(ct);
        await recordedHandler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 100m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 2),
        }, ct);

        walletBalance.Balance = 300m;
        await db.SaveChangesAsync(ct);
        await recordedHandler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 100m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 3),
        }, ct);

        // Still only 1 publication of the 25% milestone — not repeated
        eventBus.Received(1).Publish(Arg.Is<GoalMilestoneReachedEvent>(e => e.MilestonePercent == 25));
    }
}
