namespace Kakeibo.Api.Features.Identity.ExportData;

internal static partial class ExportDataLogs
{
    [LoggerMessage(2305, LogLevel.Information,
        "Export started: format={Format}, userId={UserId}")]
    internal static partial void ExportStarted(this ILogger logger, string format, Guid userId);

    [LoggerMessage(2306, LogLevel.Information,
        "Export completed: format={Format}, wallets={Wallets}, transactions={Transactions}")]
    internal static partial void ExportCompleted(this ILogger logger, string format, int wallets, int transactions);

    [LoggerMessage(2307, LogLevel.Error,
        "Export failed for user {UserId}")]
    internal static partial void ExportFailed(this ILogger logger, Guid userId, Exception exception);
}
