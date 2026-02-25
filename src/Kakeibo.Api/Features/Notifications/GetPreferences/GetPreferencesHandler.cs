using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.GetPreferences;

// Returns notification preferences for the current user, creating defaults if none exist (upsert-on-read).
public sealed class GetPreferencesHandler(AppDbContext db)
{
    public async Task<GetPreferencesEndpoint.GetPreferencesResponse> HandleAsync(
        Guid userId,
        CancellationToken ct)
    {
        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        // Upsert-on-read: if no preferences exist, create defaults and return them
        if (prefs is null)
        {
            prefs = new NotificationPreferences { UserId = userId };
            db.NotificationPreferences.Add(prefs);
            await db.SaveChangesAsync(ct);
        }

        return new GetPreferencesEndpoint.GetPreferencesResponse(
            prefs.EmailBudgetAlerts,
            prefs.EmailGoalMilestones,
            prefs.EmailInvitations,
            prefs.EmailRecurringUpdates,
            prefs.PushBudgetAlerts,
            prefs.PushGoalMilestones,
            prefs.PushInvitations,
            prefs.PushRecurringUpdates);
    }
}
