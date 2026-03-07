using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Domain.Entities;

// Metadata for a file attached to a transaction.
// Binary content is stored in MinIO at: attachments/{transactionId}/{attachmentId}/{fileName}
public sealed class TransactionAttachment : Entity
{
    public required Guid TransactionId { get; set; }
    public required string FileName { get; set; }          // original file name, max 255
    public required string ContentType { get; set; }       // MIME type, max 100
    public required long FileSizeBytes { get; set; }
    public required string ObjectName { get; set; }        // full MinIO path, max 500
    public required Guid UploadedByUserId { get; set; }

    public Transaction? Transaction { get; set; }
    public User? UploadedByUser { get; set; }
}
