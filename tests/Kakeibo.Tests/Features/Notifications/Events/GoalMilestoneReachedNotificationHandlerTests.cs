using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.Events;
using Kakeibo.Api.Features.Notifications.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kakeibo.Tests.Features.Notifications.Events;

public sealed class GoalMilestoneReachedNotificationHandlerTests
{
    private static ILogger<GoalMilestoneReachedNotificationHandler> CreateFakeLogger() =>
        Substitute.For<ILogger<GoalMilestoneReachedNotificationHandler>>();

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

        var wallet = new Wallet { Name = "Savings", Currency = "EUR" };
        db.Wallets.Add(wallet);

        var goal = new Goal
        {
            UserId = user.Id,
            WalletId = wallet.Id,
            Name = "New Car",
            TargetAmount = 10000m,
            CurrentProgress = 2500m
        };
        db.Goals.Add(goal);

        await db.SaveChangesAsync();
        return (user, goal);
    }

    [Fact]
    public async Task CreatesInAppNotification_WithMilestonePercentInBody()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, goal) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new GoalMilestoneReachedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalMilestoneReachedEvent
        {
            GoalId = goal.Id,
            UserId = user.Id,
            MilestonePercent = 25,
            CurrentProgress = 2500m,
            TargetAmount = 10000m
        }, ct);

        var notification = await db.Notifications.SingleAsync(
            n => n.UserId == user.Id && n.Type == "goal.milestone_reached", ct);

        Assert.Equal("Goal Milestone Reached", notification.Title);
        Assert.Contains("25%", notification.Body);
        Assert.Contains("New Car", notification.Body);
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
        var handler = new GoalMilestoneReachedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalMilestoneReachedEvent
        {
            GoalId = goal.Id,
            UserId = user.Id,
            MilestonePercent = 50,
            CurrentProgress = 5000m,
            TargetAmount = 10000m
        }, ct);

        await emailService.Received(1).SendGoalMilestoneEmailAsync(
            user.Id, user.Email, goal.Name, 5000m, 10000m, 50, Arg.Any<CancellationToken>());
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
        var handler = new GoalMilestoneReachedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalMilestoneReachedEvent
        {
            GoalId = goal.Id,
            UserId = user.Id,
            MilestonePercent = 50,
            CurrentProgress = 5000m,
            TargetAmount = 10000m
        }, ct);

        await emailService.DidNotReceive().SendGoalMilestoneEmailAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendsPush_WhenPushGoalMilestonesIsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, goal) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new GoalMilestoneReachedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalMilestoneReachedEvent
        {
            GoalId = goal.Id,
            UserId = user.Id,
            MilestonePercent = 75,
            CurrentProgress = 7500m,
            TargetAmount = 10000m
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
        var handler = new GoalMilestoneReachedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new GoalMilestoneReachedEvent
        {
            GoalId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            MilestonePercent = 25,
            CurrentProgress = 250m,
            TargetAmount = 1000m
        }, ct);

        var count = await db.Notifications.CountAsync(ct);
        Assert.Equal(0, count);
    }
}
