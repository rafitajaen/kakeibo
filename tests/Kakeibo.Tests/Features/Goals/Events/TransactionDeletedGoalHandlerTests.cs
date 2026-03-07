using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Kakeibo.Api.Features.Goals.Events;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Goals.Events;

public sealed class TransactionDeletedGoalHandlerTests
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
    public async Task RecalculatesProgress_AfterDeletion()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Set initial progress and a reduced balance after deletion
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.CurrentProgress = 500m;
        balance.Balance = 300m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionDeletedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionDeletedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 200m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        await db.Entry(goal).ReloadAsync(ct);
        // Progress reflects the new wallet balance after deletion
        Assert.Equal(300m, goal.CurrentProgress);
    }

    [Fact]
    public async Task DoesNotRepeatMilestone_WhenProgressDropsBelowAndStaysAboveOtherThreshold()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, balance) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, 1000m);

        // Goal was at 80% (above 75% threshold) — LastMilestone = 75
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.CurrentProgress = 800m;
        goal.LastMilestone = 75;
        // After deletion, balance drops to 600 (60% — still above 50% but below 75%)
        balance.Balance = 600m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionDeletedGoalHandler(db, eventBus);

        await handler.HandleAsync(new TransactionDeletedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 200m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        // LastMilestone was 75 — no new milestones crossed going down, no events published
        eventBus.DidNotReceive().Publish(Arg.Any<GoalMilestoneReachedEvent>());
        eventBus.DidNotReceive().Publish(Arg.Any<GoalAchievedEvent>());

        await db.Entry(goal).ReloadAsync(ct);
        Assert.Equal(600m, goal.CurrentProgress);
        // LastMilestone unchanged (progress dropped, not newly crossed)
        Assert.Equal(75, goal.LastMilestone);
    }

    [Fact]
    public async Task IgnoresGoals_WithDifferentWallet()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (walletA, _) = await CreateWalletAsync(db, user.Id, 500m);
        var (walletB, _) = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var goalId = await CreateGoalAsync(db, user.Id, walletA.Id, 1000m);

        // Set existing progress on the goal
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.CurrentProgress = 500m;
        await db.SaveChangesAsync(ct);

        var handler = new TransactionDeletedGoalHandler(db, eventBus);

        // Transaction deleted from walletB — goal on walletA unaffected
        await handler.HandleAsync(new TransactionDeletedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = walletB.Id,
            Type = "Expense",
            Amount = 100m,
            CategoryId = Guid.NewGuid(),
            Date = new LocalDate(2026, 3, 1),
        }, ct);

        await db.Entry(goal).ReloadAsync(ct);
        Assert.Equal(500m, goal.CurrentProgress);
    }
}
