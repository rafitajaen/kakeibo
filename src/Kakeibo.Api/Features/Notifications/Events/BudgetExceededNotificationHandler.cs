using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Infrastructure.WebPush;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.Events;

// Creates an in-app notification and optionally sends email/push when a budget limit is exceeded.
public sealed class BudgetExceededNotificationHandler(
    AppDbContext db,
    IEmailService emailService,
    IWebPushService pushService,
    ILogger<BudgetExceededNotificationHandler> logger)
    : IEventHandler<BudgetExceededEvent>
{
    public async Task HandleAsync(BudgetExceededEvent @event, CancellationToken cancellationToken = default)
    {
        var budget = await db.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == @event.BudgetId && b.DeletedAt == null, cancellationToken);

        if (budget is null)
        {
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == @event.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var categoryName = budget.Category?.Name ?? "Unknown";
        var title = "Budget Exceeded";
        var body = $"Your {categoryName} budget has been exceeded! Spent: {@event.CurrentSpending:F2} / Limit: {@event.Limit:F2}.";

        // Create in-app notification
        db.Notifications.Add(new Notification
        {
            UserId = @event.UserId,
            Type = NotificationTypes.BudgetExceeded,
            Title = title,
            Body = body,
            Metadata = $"{{\"budgetId\":\"{@event.BudgetId}\"}}"
        });
        await db.SaveChangesAsync(cancellationToken);

        // Load preferences (use defaults if not set)
        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == @event.UserId, cancellationToken)
            ?? new NotificationPreferences { UserId = @event.UserId };

        if (prefs.EmailBudgetAlerts)
        {
            try
            {
                // Reuse budget alert email — spending exceeds limit (percentUsed >= 100%)
                await emailService.SendBudgetAlertEmailAsync(
                    @event.UserId, user.Email, categoryName, @event.CurrentSpending, @event.Limit, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.BudgetExceededEmailFailed(@event.UserId, ex);
            }
        }

        if (prefs.PushBudgetAlerts)
        {
            try
            {
                await pushService.SendAsync(@event.UserId, title, body, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.BudgetExceededPushFailed(@event.UserId, ex);
            }
        }
    }
}
