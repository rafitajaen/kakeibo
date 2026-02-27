using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Kakeibo.Api.Features.Goals.Events;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Goals.Events;

public sealed class TransactionUpdatedGoalHandlerTests
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

    private static async Task<(Wallet wallet, WalletBalance balance)> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, decimal initialBalance = 0m)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        var walletBalance = new WalletBalance { WalletId = wallet.Id, Balance = initialBalance };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(walletBalance);
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
    public async Task RecalculatesProgress_FromWalletBalance()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 5000m);

        // Set an initial progress value
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.CurrentProgress = 1000m;
        // New wallet balance after the update is 1200
        balance.Balance = 1200m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionUpdatedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionUpdatedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 800m,
            OldAmount = 600m,
            CategoryId = Guid.NewGuid(),
            OldCategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
            OldDate = new LocalDate(2026, 3, 1),
        }, ct);

        await db.Entry(goal).ReloadAsync(ct);
        Assert.Equal(1200m, goal.CurrentProgress);
    }

    [Fact]
    public async Task PublishesMilestone_WhenUpdatePushesProgressAboveThreshold()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Balance after update is now 600 (above 50% = 500)
        balance.Balance = 600m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionUpdatedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionUpdatedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 600m,
            OldAmount = 200m,
            CategoryId = Guid.NewGuid(),
            OldCategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
            OldDate = new LocalDate(2026, 3, 1),
        }, ct);

        // 25% and 50% milestones crossed
        eventBus.Received(2).Publish(Arg.Any<GoalMilestoneReachedEvent>());
    }

    [Fact]
    public async Task DoesNotPublishMilestone_WhenLastMilestoneAlreadyRecorded()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Goal already at 50% milestone; balance stays above 50%
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.LastMilestone = 50;
        balance.Balance = 600m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionUpdatedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionUpdatedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 600m,
            OldAmount = 500m,
            CategoryId = Guid.NewGuid(),
            OldCategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
            OldDate = new LocalDate(2026, 3, 1),
        }, ct);

        // 25% and 50% already recorded — no events
        eventBus.DidNotReceive().Publish(Arg.Is<GoalMilestoneReachedEvent>(e => e.MilestonePercent <= 50));
    }
}
