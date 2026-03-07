using Microsoft.Extensions.Logging;

namespace Kakeibo.Api.Features.Transactions;

internal static partial class TransactionAttachmentLogs
{
    [LoggerMessage(3200, LogLevel.Information,
        "Attachment {AttachmentId} uploaded for transaction {TransactionId} ({FileName}) by user {UserId}")]
    internal static partial void AttachmentUploaded(
        this ILogger logger,
        Guid transactionId,
        Guid attachmentId,
        string fileName,
        Guid userId);

    [LoggerMessage(3201, LogLevel.Information,
        "Attachment {AttachmentId} deleted from transaction {TransactionId} by user {UserId}")]
    internal static partial void AttachmentDeleted(
        this ILogger logger,
        Guid transactionId,
        Guid attachmentId,
        Guid userId);

    [LoggerMessage(3202, LogLevel.Warning,
        "Failed to delete attachment {AttachmentId} from storage for transaction {TransactionId}")]
    internal static partial void AttachmentStorageDeletionFailed(
        this ILogger logger,
        Guid transactionId,
        Guid attachmentId,
        Exception exception);
}
