using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Infrastructure.WebPush;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.Events;

// Creates an in-app notification and optionally sends email/push when a savings goal is fully achieved.
public sealed class GoalAchievedNotificationHandler(
    AppDbContext db,
    IEmailService emailService,
    IWebPushService pushService,
    ILogger<GoalAchievedNotificationHandler> logger)
    : IEventHandler<GoalAchievedEvent>
{
    public async Task HandleAsync(GoalAchievedEvent @event, CancellationToken cancellationToken = default)
    {
        var goal = await db.Goals
            .FirstOrDefaultAsync(g => g.Id == @event.GoalId && g.DeletedAt == null, cancellationToken);

        if (goal is null)
        {
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == @event.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var title = "Goal Achieved!";
        var body = $"Congratulations! You've achieved your goal \"{goal.Name}\" ({@event.TargetAmount:F2}).";

        // Create in-app notification
        db.Notifications.Add(new Notification
        {
            UserId = @event.UserId,
            Type = NotificationTypes.GoalAchieved,
            Title = title,
            Body = body,
            Metadata = $"{{\"goalId\":\"{@event.GoalId}\"}}"
        });
        await db.SaveChangesAsync(cancellationToken);

        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == @event.UserId, cancellationToken)
            ?? new NotificationPreferences { UserId = @event.UserId };

        if (prefs.EmailGoalMilestones)
        {
            try
            {
                await emailService.SendGoalAchievedEmailAsync(
                    @event.UserId, user.Email, goal.Name, @event.TargetAmount, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.GoalAchievedEmailFailed(@event.UserId, ex);
            }
        }

        if (prefs.PushGoalMilestones)
        {
            try
            {
                await pushService.SendAsync(@event.UserId, title, body, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.GoalAchievedPushFailed(@event.UserId, ex);
            }
        }
    }
}
