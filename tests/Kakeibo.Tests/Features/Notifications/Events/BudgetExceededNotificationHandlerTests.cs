using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.Events;
using Kakeibo.Api.Features.Notifications.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kakeibo.Tests.Features.Notifications.Events;

public sealed class BudgetExceededNotificationHandlerTests
{
    private static ILogger<BudgetExceededNotificationHandler> CreateFakeLogger() =>
        Substitute.For<ILogger<BudgetExceededNotificationHandler>>();

    private static async Task<(User user, Budget budget)> SetupAsync(
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

        var category = new Category { Name = "Housing", UserId = null };
        db.Categories.Add(category);

        var budget = new Budget
        {
            UserId = user.Id,
            CategoryId = category.Id,
            Name = "Housing budget",
            Limit = 1000m,
            StartDate = new LocalDate(2026, 3, 1),
            EndDate = new LocalDate(2026, 3, 31),
            CurrentSpending = 1050m
        };
        db.Budgets.Add(budget);

        await db.SaveChangesAsync();
        return (user, budget);
    }

    [Fact]
    public async Task CreatesInAppNotification_WithCorrectFields()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, budget) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new BudgetExceededNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new BudgetExceededEvent
        {
            BudgetId = budget.Id,
            UserId = user.Id,
            CategoryId = budget.CategoryId,
            Limit = 1000m,
            CurrentSpending = 1050m
        }, ct);

        var notification = await db.Notifications.SingleAsync(
            n => n.UserId == user.Id && n.Type == "budget.exceeded", ct);

        Assert.Equal("Budget Exceeded", notification.Title);
        Assert.Contains("Housing", notification.Body);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task SendsEmail_WhenEmailBudgetAlertsIsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, budget) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new BudgetExceededNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new BudgetExceededEvent
        {
            BudgetId = budget.Id,
            UserId = user.Id,
            CategoryId = budget.CategoryId,
            Limit = 1000m,
            CurrentSpending = 1050m
        }, ct);

        await emailService.Received(1).SendBudgetAlertEmailAsync(
            user.Id, user.Email, Arg.Any<string>(), 1050m, 1000m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotSendEmail_WhenEmailBudgetAlertsIsDisabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, budget) = await SetupAsync(db);

        db.NotificationPreferences.Add(new NotificationPreferences
        {
            UserId = user.Id,
            EmailBudgetAlerts = false
        });
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new BudgetExceededNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new BudgetExceededEvent
        {
            BudgetId = budget.Id,
            UserId = user.Id,
            CategoryId = budget.CategoryId,
            Limit = 1000m,
            CurrentSpending = 1050m
        }, ct);

        await emailService.DidNotReceive().SendBudgetAlertEmailAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendsPush_WhenPushBudgetAlertsIsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, budget) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new BudgetExceededNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new BudgetExceededEvent
        {
            BudgetId = budget.Id,
            UserId = user.Id,
            CategoryId = budget.CategoryId,
            Limit = 1000m,
            CurrentSpending = 1050m
        }, ct);

        await pushService.Received(1).SendAsync(user.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotSendPush_WhenPushBudgetAlertsIsDisabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, budget) = await SetupAsync(db);

        db.NotificationPreferences.Add(new NotificationPreferences
        {
            UserId = user.Id,
            PushBudgetAlerts = false
        });
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new BudgetExceededNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new BudgetExceededEvent
        {
            BudgetId = budget.Id,
            UserId = user.Id,
            CategoryId = budget.CategoryId,
            Limit = 1000m,
            CurrentSpending = 1050m
        }, ct);

        await pushService.DidNotReceive().SendAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNothing_WhenBudgetDoesNotExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new BudgetExceededNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new BudgetExceededEvent
        {
            BudgetId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Limit = 1000m,
            CurrentSpending = 1050m
        }, ct);

        var count = await db.Notifications.CountAsync(ct);
        Assert.Equal(0, count);
    }
}
