namespace Kakeibo.Api.Infrastructure.Email;

internal static partial class EmailServiceLogs
{
    [LoggerMessage(1201, LogLevel.Information,
        "Verification email sent to {Email} for user {UserId}")]
    internal static partial void VerificationEmailSent(this ILogger logger, string email, Guid userId);

    [LoggerMessage(1202, LogLevel.Error,
        "Failed to send verification email to {Email} for user {UserId}")]
    internal static partial void VerificationEmailFailed(this ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1203, LogLevel.Information,
        "Welcome email sent to {Email} for user {UserId}")]
    internal static partial void WelcomeEmailSent(this ILogger logger, string email, Guid userId);

    [LoggerMessage(1204, LogLevel.Error,
        "Failed to send welcome email to {Email} for user {UserId}")]
    internal static partial void WelcomeEmailFailed(this ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1205, LogLevel.Information,
        "Password reset email sent to {Email} for user {UserId}")]
    internal static partial void PasswordResetEmailSent(this ILogger logger, string email, Guid userId);

    [LoggerMessage(1206, LogLevel.Error,
        "Failed to send password reset email to {Email} for user {UserId}")]
    internal static partial void PasswordResetEmailFailed(this ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1207, LogLevel.Information,
        "Wallet invitation email sent to {Email} for invitation {InvitationId}")]
    internal static partial void WalletInvitationEmailSent(this ILogger logger, string email, Guid invitationId);

    [LoggerMessage(1208, LogLevel.Error,
        "Failed to send wallet invitation email to {Email} for invitation {InvitationId}")]
    internal static partial void WalletInvitationEmailFailed(this ILogger logger, string email, Guid invitationId, Exception exception);

    [LoggerMessage(1209, LogLevel.Information,
        "Budget alert email sent to {Email} for user {UserId}")]
    internal static partial void BudgetAlertEmailSent(this ILogger logger, string email, Guid userId);

    [LoggerMessage(1210, LogLevel.Error,
        "Failed to send budget alert email to {Email} for user {UserId}")]
    internal static partial void BudgetAlertEmailFailed(this ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1211, LogLevel.Information,
        "Goal milestone email sent to {Email} for user {UserId}")]
    internal static partial void GoalMilestoneEmailSent(this ILogger logger, string email, Guid userId);

    [LoggerMessage(1212, LogLevel.Error,
        "Failed to send goal milestone email to {Email} for user {UserId}")]
    internal static partial void GoalMilestoneEmailFailed(this ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1213, LogLevel.Information,
        "Goal achieved email sent to {Email} for user {UserId}")]
    internal static partial void GoalAchievedEmailSent(this ILogger logger, string email, Guid userId);

    [LoggerMessage(1214, LogLevel.Error,
        "Failed to send goal achieved email to {Email} for user {UserId}")]
    internal static partial void GoalAchievedEmailFailed(this ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1215, LogLevel.Information,
        "Member joined email sent to {Email} for user {UserId}")]
    internal static partial void MemberJoinedEmailSent(this ILogger logger, string email, Guid userId);

    [LoggerMessage(1216, LogLevel.Error,
        "Failed to send member joined email to {Email} for user {UserId}")]
    internal static partial void MemberJoinedEmailFailed(this ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1217, LogLevel.Information,
        "Recurring generated email sent to {Email} for user {UserId}")]
    internal static partial void RecurringGeneratedEmailSent(this ILogger logger, string email, Guid userId);

    [LoggerMessage(1218, LogLevel.Error,
        "Failed to send recurring generated email to {Email} for user {UserId}")]
    internal static partial void RecurringGeneratedEmailFailed(this ILogger logger, string email, Guid userId, Exception exception);
}
