using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Notifications.UpdatePreferences;

// Upserts notification channel preferences for the current user.
public sealed class UpdatePreferencesHandler(AppDbContext db, IClock clock)
{
    public async Task HandleAsync(
        UpdatePreferencesEndpoint.UpdatePreferencesRequest request,
        Guid userId,
        CancellationToken ct)
    {
        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (prefs is null)
        {
            prefs = new NotificationPreferences { UserId = userId };
            db.NotificationPreferences.Add(prefs);
        }

        prefs.EmailBudgetAlerts = request.EmailBudgetAlerts;
        prefs.EmailGoalMilestones = request.EmailGoalMilestones;
        prefs.EmailInvitations = request.EmailInvitations;
        prefs.EmailRecurringUpdates = request.EmailRecurringUpdates;
        prefs.PushBudgetAlerts = request.PushBudgetAlerts;
        prefs.PushGoalMilestones = request.PushGoalMilestones;
        prefs.PushInvitations = request.PushInvitations;
        prefs.PushRecurringUpdates = request.PushRecurringUpdates;
        prefs.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);
    }
}
