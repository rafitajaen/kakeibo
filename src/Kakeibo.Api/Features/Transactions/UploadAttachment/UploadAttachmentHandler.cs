using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Infrastructure.Storage;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Kakeibo.Api.Features.Transactions.UploadAttachment;

// Uploads a file attachment for a transaction to MinIO and saves the metadata in the database.
public sealed class UploadAttachmentHandler(
    AppDbContext db,
    IStorageService storage,
    ILogger<UploadAttachmentHandler> logger)
{
    private static readonly long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public async Task<Result<UploadAttachmentEndpoint.UploadAttachmentResponse>> HandleAsync(
        Guid transactionId,
        IFormFile file,
        Guid userId,
        CancellationToken ct)
    {
        // Validate file type and size before touching storage or DB
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return Error.Validation("Attachment must be a PDF, JPEG, PNG, or WebP file.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return Error.Validation("Attachment must be 10 MB or smaller.");
        }

        // Verify the transaction exists and the user has access through its wallet
        var transaction = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.DeletedAt == null, ct);

        if (transaction is null)
        {
            return Error.NotFound("Transaction not found.");
        }

        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == transaction.WalletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.NotFound("Transaction not found.");
        }

        var isOwner = wallet.OwnerId == userId;
        var isMember = await db.WalletMembers
            .AnyAsync(m => m.WalletId == transaction.WalletId && m.UserId == userId, ct);

        if (!isOwner && !isMember)
        {
            return Error.Forbidden("You do not have access to this transaction.");
        }

        await storage.EnsureBucketExistsAsync(BucketNames.Attachments, ct);

        // Sanitize the file name to prevent path traversal — keep only the final segment.
        var safeFileName = Path.GetFileName(file.FileName);
        var attachmentId = Guid7.NewGuid();

        // Object path: attachments/{transactionId}/{attachmentId}/{fileName}
        var objectName = $"{transactionId}/{attachmentId}/{safeFileName}";

        using var stream = file.OpenReadStream();
        await storage.UploadFileAsync(BucketNames.Attachments, objectName, stream, file.ContentType, ct);

        var attachment = new TransactionAttachment
        {
            Id = attachmentId,
            TransactionId = transactionId,
            FileName = safeFileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            ObjectName = objectName,
            UploadedByUserId = userId
        };

        db.TransactionAttachments.Add(attachment);
        await db.SaveChangesAsync(ct);

        logger.AttachmentUploaded(transactionId, attachmentId, safeFileName, userId);

        return new UploadAttachmentEndpoint.UploadAttachmentResponse(
            attachment.Id,
            attachment.TransactionId,
            attachment.FileName,
            attachment.ContentType,
            attachment.FileSizeBytes,
            attachment.UploadedByUserId,
            attachment.CreatedAt);
    }
}
