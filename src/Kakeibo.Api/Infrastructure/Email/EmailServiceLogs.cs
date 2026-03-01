namespace Kakeibo.Api.Infrastructure.Email;

internal static partial class EmailServiceLogs
{
    [LoggerMessage(1201, LogLevel.Information,
        "Verification email sent to {Email} for user {UserId}")]
    internal static partial void VerificationEmailSent(ILogger logger, string email, Guid userId);

    [LoggerMessage(1202, LogLevel.Error,
        "Failed to send verification email to {Email} for user {UserId}")]
    internal static partial void VerificationEmailFailed(ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1203, LogLevel.Information,
        "Welcome email sent to {Email} for user {UserId}")]
    internal static partial void WelcomeEmailSent(ILogger logger, string email, Guid userId);

    [LoggerMessage(1204, LogLevel.Error,
        "Failed to send welcome email to {Email} for user {UserId}")]
    internal static partial void WelcomeEmailFailed(ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1205, LogLevel.Information,
        "Password reset email sent to {Email} for user {UserId}")]
    internal static partial void PasswordResetEmailSent(ILogger logger, string email, Guid userId);

    [LoggerMessage(1206, LogLevel.Error,
        "Failed to send password reset email to {Email} for user {UserId}")]
    internal static partial void PasswordResetEmailFailed(ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1207, LogLevel.Information,
        "Wallet invitation email sent to {Email} for invitation {InvitationId}")]
    internal static partial void WalletInvitationEmailSent(ILogger logger, string email, Guid invitationId);

    [LoggerMessage(1208, LogLevel.Error,
        "Failed to send wallet invitation email to {Email} for invitation {InvitationId}")]
    internal static partial void WalletInvitationEmailFailed(ILogger logger, string email, Guid invitationId, Exception exception);

    [LoggerMessage(1209, LogLevel.Information,
        "Budget alert email sent to {Email} for user {UserId}")]
    internal static partial void BudgetAlertEmailSent(ILogger logger, string email, Guid userId);

    [LoggerMessage(1210, LogLevel.Error,
        "Failed to send budget alert email to {Email} for user {UserId}")]
    internal static partial void BudgetAlertEmailFailed(ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1211, LogLevel.Information,
        "Goal milestone email sent to {Email} for user {UserId}")]
    internal static partial void GoalMilestoneEmailSent(ILogger logger, string email, Guid userId);

    [LoggerMessage(1212, LogLevel.Error,
        "Failed to send goal milestone email to {Email} for user {UserId}")]
    internal static partial void GoalMilestoneEmailFailed(ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1213, LogLevel.Information,
        "Goal achieved email sent to {Email} for user {UserId}")]
    internal static partial void GoalAchievedEmailSent(ILogger logger, string email, Guid userId);

    [LoggerMessage(1214, LogLevel.Error,
        "Failed to send goal achieved email to {Email} for user {UserId}")]
    internal static partial void GoalAchievedEmailFailed(ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1215, LogLevel.Information,
        "Member joined email sent to {Email} for user {UserId}")]
    internal static partial void MemberJoinedEmailSent(ILogger logger, string email, Guid userId);

    [LoggerMessage(1216, LogLevel.Error,
        "Failed to send member joined email to {Email} for user {UserId}")]
    internal static partial void MemberJoinedEmailFailed(ILogger logger, string email, Guid userId, Exception exception);

    [LoggerMessage(1217, LogLevel.Information,
        "Recurring generated email sent to {Email} for user {UserId}")]
    internal static partial void RecurringGeneratedEmailSent(ILogger logger, string email, Guid userId);

    [LoggerMessage(1218, LogLevel.Error,
        "Failed to send recurring generated email to {Email} for user {UserId}")]
    internal static partial void RecurringGeneratedEmailFailed(ILogger logger, string email, Guid userId, Exception exception);
}
