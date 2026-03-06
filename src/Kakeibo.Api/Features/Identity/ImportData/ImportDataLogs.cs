namespace Kakeibo.Api.Features.Identity.ImportData;

internal static partial class ImportDataLogs
{
    [LoggerMessage(2301, LogLevel.Information,
        "Import started: source={Source}, file={FileName}, userId={UserId}")]
    internal static partial void ImportStarted(this ILogger logger, string source, string fileName, Guid userId);

    [LoggerMessage(2302, LogLevel.Information,
        "Import completed: source={Source}, format={Format}, wallets={Wallets}, transactions={Transactions}")]
    internal static partial void ImportCompleted(this ILogger logger, string source, string format, int wallets, int transactions);

    [LoggerMessage(2303, LogLevel.Warning,
        "Import: skipped {Count} shared wallet members")]
    internal static partial void ImportSkippedMembers(this ILogger logger, int count);

    [LoggerMessage(2304, LogLevel.Error,
        "Import failed: source={Source}, file={FileName}")]
    internal static partial void ImportFailed(this ILogger logger, string source, string fileName, Exception exception);

    [LoggerMessage(2305, LogLevel.Information,
        "Export started: format={Format}, userId={UserId}")]
    internal static partial void ExportStarted(this ILogger logger, string format, Guid userId);

    [LoggerMessage(2306, LogLevel.Information,
        "Export completed: format={Format}, wallets={Wallets}, transactions={Transactions}")]
    internal static partial void ExportCompleted(this ILogger logger, string format, int wallets, int transactions);
}
