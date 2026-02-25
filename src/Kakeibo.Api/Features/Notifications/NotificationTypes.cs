namespace Kakeibo.Api.Features.Notifications;

// Notification type constants used when creating in-app notifications.
public static class NotificationTypes
{
    public const string BudgetWarning = "budget.warning";
    public const string BudgetExceeded = "budget.exceeded";
    public const string GoalMilestoneReached = "goal.milestone";
    public const string GoalAchieved = "goal.achieved";
    public const string InvitationSent = "invitation.sent";
    public const string MemberJoined = "wallet.member_joined";
    public const string SettlementRecorded = "settlement.recorded";
    public const string RecurringTransactionGenerated = "recurring.generated";
}
