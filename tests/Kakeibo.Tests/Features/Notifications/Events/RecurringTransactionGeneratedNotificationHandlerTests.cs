using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Notifications.Events;
using Kakeibo.Api.Features.Recurring.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kakeibo.Tests.Features.Notifications.Events;

public sealed class RecurringTransactionGeneratedNotificationHandlerTests
{
    private static ILogger<RecurringTransactionGeneratedNotificationHandler> CreateFakeLogger() =>
        Substitute.For<ILogger<RecurringTransactionGeneratedNotificationHandler>>();

    private static async Task<(User user, RecurringPattern pattern)> SetupAsync(
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

        var wallet = new Wallet { Name = "Checking", Currency = "EUR" };
        db.Wallets.Add(wallet);

        await db.SaveChangesAsync();

        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "Monthly Rent",
            Amount = 1200m,
            Description = "Rent payment",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Monthly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 1, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        };
        db.RecurringPatterns.Add(pattern);

        await db.SaveChangesAsync();
        return (user, pattern);
    }

    [Fact]
    public async Task CreatesInAppNotification_WithCorrectFields()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, pattern) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new RecurringTransactionGeneratedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new RecurringTransactionGeneratedEvent
        {
            RecurringPatternId = pattern.Id,
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            OccurredAt = SystemClock.Instance.GetCurrentInstant()
        }, ct);

        var notification = await db.Notifications.SingleAsync(
            n => n.UserId == user.Id && n.Type == "recurring.generated", ct);

        Assert.Equal("Recurring Transaction Generated", notification.Title);
        Assert.Contains("Monthly Rent", notification.Body);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task SendsEmail_WhenEmailRecurringUpdatesIsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, pattern) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new RecurringTransactionGeneratedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new RecurringTransactionGeneratedEvent
        {
            RecurringPatternId = pattern.Id,
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            OccurredAt = SystemClock.Instance.GetCurrentInstant()
        }, ct);

        await emailService.Received(1).SendRecurringTransactionGeneratedEmailAsync(
            user.Id, user.Email, pattern.Name, pattern.Amount, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotSendEmail_WhenEmailRecurringUpdatesIsDisabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, pattern) = await SetupAsync(db);

        db.NotificationPreferences.Add(new NotificationPreferences
        {
            UserId = user.Id,
            EmailRecurringUpdates = false
        });
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new RecurringTransactionGeneratedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new RecurringTransactionGeneratedEvent
        {
            RecurringPatternId = pattern.Id,
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            OccurredAt = SystemClock.Instance.GetCurrentInstant()
        }, ct);

        await emailService.DidNotReceive().SendRecurringTransactionGeneratedEmailAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendsPush_WhenPushRecurringUpdatesIsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, pattern) = await SetupAsync(db);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new RecurringTransactionGeneratedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new RecurringTransactionGeneratedEvent
        {
            RecurringPatternId = pattern.Id,
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            OccurredAt = SystemClock.Instance.GetCurrentInstant()
        }, ct);

        await pushService.Received(1).SendAsync(
            user.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNothing_WhenRecurringPatternDoesNotExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new RecurringTransactionGeneratedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new RecurringTransactionGeneratedEvent
        {
            RecurringPatternId = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OccurredAt = SystemClock.Instance.GetCurrentInstant()
        }, ct);

        var count = await db.Notifications.CountAsync(ct);
        Assert.Equal(0, count);
    }
}
