using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Infrastructure.WebPush;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kakeibo.Api.Features.Notifications.Events;

// Creates an in-app notification and optionally sends email/push when a goal milestone is reached.
public sealed class GoalMilestoneReachedNotificationHandler(
    AppDbContext db,
    IEmailService emailService,
    IWebPushService pushService,
    ILogger<GoalMilestoneReachedNotificationHandler> logger)
    : IEventHandler<GoalMilestoneReachedEvent>
{
    public async Task HandleAsync(GoalMilestoneReachedEvent @event, CancellationToken cancellationToken = default)
    {
        var goal = await db.Goals
            .FirstOrDefaultAsync(g => g.Id == @event.GoalId && g.DeletedAt == null, cancellationToken);

        if (goal is null)
            return;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == @event.UserId, cancellationToken);
        if (user is null)
            return;

        var title = "Goal Milestone Reached";
        var body = $"You've reached {@event.MilestonePercent}% of your goal \"{goal.Name}\"! ({@event.CurrentProgress:F2} / {@event.TargetAmount:F2})";

        // Create in-app notification
        db.Notifications.Add(new Notification
        {
            UserId = @event.UserId,
            Type = NotificationTypes.GoalMilestoneReached,
            Title = title,
            Body = body,
            Metadata = $"{{\"goalId\":\"{@event.GoalId}\",\"milestone\":{@event.MilestonePercent}}}"
        });
        await db.SaveChangesAsync(cancellationToken);

        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == @event.UserId, cancellationToken)
            ?? new NotificationPreferences { UserId = @event.UserId };

        if (prefs.EmailGoalMilestones)
        {
            try
            {
                await emailService.SendGoalMilestoneEmailAsync(
                    @event.UserId, user.Email, goal.Name,
                    @event.CurrentProgress, @event.TargetAmount, @event.MilestonePercent, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send goal milestone email to user {UserId}", @event.UserId);
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
                logger.LogWarning(ex, "Failed to send goal milestone push to user {UserId}", @event.UserId);
            }
        }
    }
}
