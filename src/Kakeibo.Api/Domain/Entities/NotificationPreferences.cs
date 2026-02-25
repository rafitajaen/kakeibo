using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

// Stores per-user notification channel preferences.
// Uses UserId as primary key (1:1 with User). Does not inherit Entity — no soft delete needed.
public sealed class NotificationPreferences
{
    public required Guid UserId { get; set; }

    // Email notification preferences
    public bool EmailBudgetAlerts { get; set; } = true;
    public bool EmailGoalMilestones { get; set; } = true;
    public bool EmailInvitations { get; set; } = true;
    public bool EmailRecurringUpdates { get; set; } = true;

    // Push notification preferences
    public bool PushBudgetAlerts { get; set; } = true;
    public bool PushGoalMilestones { get; set; } = true;
    public bool PushInvitations { get; set; } = true;
    public bool PushRecurringUpdates { get; set; } = true;

    public Instant UpdatedAt { get; set; } = SystemClock.Instance.GetCurrentInstant();

    // Navigation property
    public User? User { get; set; }
}
