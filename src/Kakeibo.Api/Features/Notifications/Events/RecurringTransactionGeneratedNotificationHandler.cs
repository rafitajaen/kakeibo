using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Recurring.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Infrastructure.WebPush;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.Events;

// Creates an in-app notification and optionally sends email/push when a recurring transaction is auto-generated.
public sealed class RecurringTransactionGeneratedNotificationHandler(
    AppDbContext db,
    IEmailService emailService,
    IWebPushService pushService,
    ILogger<RecurringTransactionGeneratedNotificationHandler> logger)
    : IEventHandler<RecurringTransactionGeneratedEvent>
{
    public async Task HandleAsync(
        RecurringTransactionGeneratedEvent @event,
        CancellationToken cancellationToken = default)
    {
        var pattern = await db.RecurringPatterns
            .FirstOrDefaultAsync(p => p.Id == @event.RecurringPatternId && p.DeletedAt == null, cancellationToken);

        if (pattern is null)
        {
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == @event.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var title = "Recurring Transaction Generated";
        var body = $"A recurring transaction \"{pattern.Name}\" ({pattern.Amount:F2}) was automatically generated.";

        // Create in-app notification
        db.Notifications.Add(new Notification
        {
            UserId = @event.UserId,
            Type = NotificationTypes.RecurringTransactionGenerated,
            Title = title,
            Body = body,
            Metadata = $"{{\"patternId\":\"{@event.RecurringPatternId}\",\"transactionId\":\"{@event.TransactionId}\"}}"
        });
        await db.SaveChangesAsync(cancellationToken);

        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == @event.UserId, cancellationToken)
            ?? new NotificationPreferences { UserId = @event.UserId };

        if (prefs.EmailRecurringUpdates)
        {
            try
            {
                await emailService.SendRecurringTransactionGeneratedEmailAsync(
                    @event.UserId, user.Email, pattern.Name, pattern.Amount, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.RecurringGeneratedEmailFailed(@event.UserId, ex);
            }
        }

        if (prefs.PushRecurringUpdates)
        {
            try
            {
                await pushService.SendAsync(@event.UserId, title, body, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.RecurringGeneratedPushFailed(@event.UserId, ex);
            }
        }
    }
}
