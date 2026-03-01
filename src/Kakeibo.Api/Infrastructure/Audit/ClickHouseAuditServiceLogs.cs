namespace Kakeibo.Api.Infrastructure.Audit;

internal static partial class ClickHouseAuditServiceLogs
{
    [LoggerMessage(1101, LogLevel.Debug,
        "ClickHouse unavailable — skipping audit event. Action={Action}, UserId={UserId}")]
    internal static partial void ClickHouseUnavailableSkipping(ILogger logger, string action, Guid userId);

    [LoggerMessage(1102, LogLevel.Warning,
        "ClickHouse unavailable — audit events will be skipped until restart. Action={Action}, UserId={UserId}")]
    internal static partial void ClickHouseUnavailableAuditFailed(ILogger logger, string action, Guid userId, Exception exception);

    [LoggerMessage(1103, LogLevel.Debug,
        "ClickHouse unavailable — returning empty activity feed for user {UserId}")]
    internal static partial void ClickHouseUnavailableActivityFeed(ILogger logger, Guid userId);

    [LoggerMessage(1104, LogLevel.Warning,
        "ClickHouse unavailable — activity feed will return empty until restart. UserId={UserId}")]
    internal static partial void ClickHouseUnavailableActivityFeedFailed(ILogger logger, Guid userId, Exception exception);
}
