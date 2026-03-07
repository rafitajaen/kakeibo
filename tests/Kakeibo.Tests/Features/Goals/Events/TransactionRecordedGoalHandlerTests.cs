using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Kakeibo.Api.Features.Goals.Events;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Goals.Events;

public sealed class TransactionRecordedGoalHandlerTests
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

    private static async Task<(Wallet wallet, WalletBalance balance)> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, decimal initialBalance = 0m)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", Currency = "EUR" };
        var walletBalance = new WalletBalance { WalletId = wallet.Id, Balance = initialBalance };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(walletBalance);
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = ownerId, Role = WalletMemberRole.Owner });
        await db.SaveChangesAsync();
        return (wallet, walletBalance);
    }

    private static async Task<Guid> CreateGoalAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId,
        Guid walletId,
        decimal targetAmount)
    {
        var handler = new CreateGoalHandler(db, TestClock);
        var result = await handler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest("Test Goal", targetAmount, null, walletId),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    [Fact]
    public async Task RespondsToAllTransactionTypes_NotOnlyExpense()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Set wallet balance to 200
        balance.Balance = 200m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus);

        // Income transaction — should still update progress
        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 200m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        var goal = await db.Goals.FindAsync([goalId], ct);
        Assert.Equal(200m, goal!.CurrentProgress);
    }

    [Fact]
    public async Task UpdatesProgress_FromWalletBalance()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 5000m);

        // Simulate wallet balance after a transaction
        balance.Balance = 1500m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 500m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        var goal = await db.Goals.FindAsync([goalId], ct);
        Assert.Equal(1500m, goal!.CurrentProgress);
    }

    [Fact]
    public async Task IgnoresGoals_WithDifferentWallet()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (walletA, balanceA) = await CreateWalletAsync(db, user.Id);
        var (walletB, _) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        // Goal linked to walletA
        var goalId = await CreateGoalAsync(db, user.Id, walletA.Id, 1000m);

        balanceA.Balance = 500m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus);

        // Transaction on walletB — goal for walletA should not be updated
        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = walletB.Id,
            Type = "Income",
            Amount = 500m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        var goal = await db.Goals.FindAsync([goalId], ct);
        Assert.Equal(0m, goal!.CurrentProgress);
    }

    [Fact]
    public async Task PublishesMilestoneEvent_WhenCrossing25Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Balance crosses 25% (250)
        balance.Balance = 300m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 300m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        eventBus.Received(1).Publish(Arg.Is<GoalMilestoneReachedEvent>(e =>
            e.GoalId == goalId && e.MilestonePercent == 25));
        eventBus.DidNotReceive().Publish(Arg.Any<GoalAchievedEvent>());

        var goal = await db.Goals.FindAsync([goalId], ct);
        Assert.Equal(25, goal!.LastMilestone);
    }

    [Fact]
    public async Task PublishesAchievedEvent_WhenCrossing100Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Balance exactly at target
        balance.Balance = 1000m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 1000m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        // All milestones crossed in one jump: 25, 50, 75, plus GoalAchieved
        eventBus.Received(3).Publish(Arg.Any<GoalMilestoneReachedEvent>());
        eventBus.Received(1).Publish(Arg.Any<GoalAchievedEvent>());

        var goal = await db.Goals.FindAsync([goalId], ct);
        Assert.Equal(100, goal!.LastMilestone);
    }

    [Fact]
    public async Task DoesNotRepeatMilestone_WhenAlreadyAboveThreshold()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Simulate goal already at 30% with LastMilestone = 25
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.CurrentProgress = 300m;
        goal.LastMilestone = 25;
        balance.Balance = 400m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 100m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        // 25% already reached — no duplicate, only 50% has not been crossed
        eventBus.DidNotReceive().Publish(Arg.Is<GoalMilestoneReachedEvent>(e => e.MilestonePercent == 25));
        eventBus.DidNotReceive().Publish(Arg.Is<GoalMilestoneReachedEvent>(e => e.MilestonePercent == 50));
    }

    [Fact]
    public async Task SkipsDeletedGoals()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Soft-delete the goal
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.DeletedAt = SystemClock.Instance.GetCurrentInstant();
        balance.Balance = 500m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionRecordedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 500m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        // Progress should not be updated — goal is deleted
        await db.Entry(goal).ReloadAsync(ct);
        Assert.Equal(0m, goal.CurrentProgress);
    }
}
