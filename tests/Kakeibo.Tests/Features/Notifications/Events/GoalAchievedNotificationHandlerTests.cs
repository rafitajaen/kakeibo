using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.Events;
using Kakeibo.Api.Features.Notifications.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kakeibo.Tests.Features.Notifications.Events;

public sealed class GoalAchievedNotificationHandlerTests
{
    private static ILogger<GoalAchievedNotificationHandler> CreateFakeLogger() =>
        Substitute.For<ILogger<GoalAchievedNotificationHandler>>();

    private static async Task<(User user, Goal goal)> SetupAsync(
        Kakeibo.Api.Persistence.AppDbContext db)
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

        var wallet = new Wallet { Name = "Savings", OwnerId = user.Id, Currency = "EUR" };
        db.Wallets.Add(wallet);

        var goal = new Goal
        {
            UserId = user.Id,
            WalletId = wallet.Id,
            Name = "Europe Trip",
            TargetAmount = 5000m,
            CurrentProgress = 5000m
        };
        db.Goals.Add(goal);

        await db.SaveChangesAsync();
        return (user, goal);
    }

    [Fact]
    public async Task CreatesInAppNotification_WithCorrectFields()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, goal) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new GoalAchievedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalAchievedEvent
        {
            GoalId = goal.Id,
            UserId = user.Id,
            TargetAmount = 5000m,
            CurrentProgress = 5000m
        }, ct);

        var notification = await db.Notifications.SingleAsync(
            n => n.UserId == user.Id && n.Type == "goal.achieved", ct);

        Assert.Equal("Goal Achieved!", notification.Title);
        Assert.Contains("Europe Trip", notification.Body);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task SendsEmail_WhenEmailGoalMilestonesIsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, goal) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new GoalAchievedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalAchievedEvent
        {
            GoalId = goal.Id,
            UserId = user.Id,
            TargetAmount = 5000m,
            CurrentProgress = 5000m
        }, ct);

        await emailService.Received(1).SendGoalAchievedEmailAsync(
            user.Id, user.Email, goal.Name, 5000m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotSendEmail_WhenEmailGoalMilestonesIsDisabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, goal) = await SetupAsync(db);

        db.NotificationPreferences.Add(new NotificationPreferences
        {
            UserId = user.Id,
            EmailGoalMilestones = false
        });
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new GoalAchievedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalAchievedEvent
        {
            GoalId = goal.Id,
            UserId = user.Id,
            TargetAmount = 5000m,
            CurrentProgress = 5000m
        }, ct);

        await emailService.DidNotReceive().SendGoalAchievedEmailAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendsPush_WhenPushGoalMilestonesIsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, goal) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new GoalAchievedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalAchievedEvent
        {
            GoalId = goal.Id,
            UserId = user.Id,
            TargetAmount = 5000m,
            CurrentProgress = 5000m
        }, ct);

        await pushService.Received(1).SendAsync(
            user.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNothing_WhenGoalDoesNotExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new GoalAchievedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalAchievedEvent
        {
            GoalId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TargetAmount = 5000m,
            CurrentProgress = 5000m
        }, ct);

        var count = await db.Notifications.CountAsync(ct);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DoesNothing_WhenGoalIsSoftDeleted()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, goal) = await SetupAsync(db);

        goal.DeletedAt = SystemClock.Instance.GetCurrentInstant();
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new GoalAchievedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalAchievedEvent
        {
            GoalId = goal.Id,
            UserId = user.Id,
            TargetAmount = 5000m,
            CurrentProgress = 5000m
        }, ct);

        var count = await db.Notifications.CountAsync(ct);
        Assert.Equal(0, count);
    }
}
