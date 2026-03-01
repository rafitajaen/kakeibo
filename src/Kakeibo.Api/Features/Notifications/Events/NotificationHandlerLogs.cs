namespace Kakeibo.Api.Features.Notifications.Events;

internal static partial class NotificationHandlerLogs
{
    [LoggerMessage(3100, LogLevel.Warning,
        "Failed to send budget exceeded email to user {UserId}")]
    internal static partial void BudgetExceededEmailFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3101, LogLevel.Warning,
        "Failed to send budget exceeded push to user {UserId}")]
    internal static partial void BudgetExceededPushFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3102, LogLevel.Warning,
        "Failed to send budget warning email to user {UserId}")]
    internal static partial void BudgetWarningEmailFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3103, LogLevel.Warning,
        "Failed to send budget warning push to user {UserId}")]
    internal static partial void BudgetWarningPushFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3104, LogLevel.Warning,
        "Failed to send goal achieved email to user {UserId}")]
    internal static partial void GoalAchievedEmailFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3105, LogLevel.Warning,
        "Failed to send goal achieved push to user {UserId}")]
    internal static partial void GoalAchievedPushFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3106, LogLevel.Warning,
        "Failed to send goal milestone email to user {UserId}")]
    internal static partial void GoalMilestoneEmailFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3107, LogLevel.Warning,
        "Failed to send goal milestone push to user {UserId}")]
    internal static partial void GoalMilestonePushFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3108, LogLevel.Warning,
        "Failed to send invitation email for invitation {InvitationId}")]
    internal static partial void InvitationSentEmailFailed(this ILogger logger, Guid invitationId, Exception exception);

    [LoggerMessage(3109, LogLevel.Warning,
        "Failed to send invitation push to user {UserId}")]
    internal static partial void InvitationSentPushFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3110, LogLevel.Warning,
        "Failed to send member joined email to user {UserId}")]
    internal static partial void MemberJoinedEmailFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3111, LogLevel.Warning,
        "Failed to send member joined push to user {UserId}")]
    internal static partial void MemberJoinedPushFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3112, LogLevel.Warning,
        "Failed to send recurring generated email to user {UserId}")]
    internal static partial void RecurringGeneratedEmailFailed(this ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(3113, LogLevel.Warning,
        "Failed to send recurring generated push to user {UserId}")]
    internal static partial void RecurringGeneratedPushFailed(this ILogger logger, Guid userId, Exception exception);
}
