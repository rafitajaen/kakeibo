namespace Kakeibo.Api.Infrastructure.WebPush;

internal static partial class WebPushServiceLogs
{
    [LoggerMessage(1401, LogLevel.Information,
        "Removed expired push subscription for user {UserId}")]
    internal static partial void ExpiredSubscriptionRemoved(this ILogger logger, Guid userId);

    [LoggerMessage(1402, LogLevel.Warning,
        "Failed to send push notification to user {UserId}")]
    internal static partial void PushNotificationFailed(this ILogger logger, Guid userId, Exception exception);
}
