using NodaTime;

namespace Kakeibo.Api.Features.Recurring.Jobs;

internal static partial class GenerateRecurringTransactionsJobLogs
{
    [LoggerMessage(2201, LogLevel.Information,
        "GenerateRecurringTransactionsJob: {Count} pattern(s) due on {Today}")]
    internal static partial void PatternsDue(this ILogger logger, int count, LocalDate today);

    [LoggerMessage(2202, LogLevel.Error,
        "GenerateRecurringTransactionsJob: unhandled error processing pattern {PatternId}")]
    internal static partial void PatternProcessingFailed(this ILogger logger, Guid patternId, Exception exception);

    [LoggerMessage(2203, LogLevel.Debug,
        "Generated transaction {TransactionId} for pattern {PatternId} on {Date}")]
    internal static partial void TransactionGenerated(this ILogger logger, Guid transactionId, Guid patternId, LocalDate date);

    [LoggerMessage(2204, LogLevel.Warning,
        "Failed to generate transaction for pattern {PatternId} on {Date}: {Error}")]
    internal static partial void TransactionGenerationFailed(this ILogger logger, Guid patternId, LocalDate date, string error);
}
