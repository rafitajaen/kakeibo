namespace Kakeibo.Api.Features.Identity.Jobs;

internal static partial class AccountDeletionJobLogs
{
    [LoggerMessage(2101, LogLevel.Information,
        "AccountDeletionJob: {Count} account(s) pending permanent deletion")]
    internal static partial void PendingDeletionAccounts(ILogger logger, int count);
}
