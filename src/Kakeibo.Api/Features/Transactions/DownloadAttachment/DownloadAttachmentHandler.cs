using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Infrastructure.Storage;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Transactions.DownloadAttachment;

// Downloads a transaction attachment from MinIO and returns it as a binary stream.
public sealed class DownloadAttachmentHandler(AppDbContext db, IStorageService storage)
{
    // Transfer object — endpoint uses it to build the file response.
    public sealed record AttachmentDownload(Stream Stream, string ContentType, string FileName);

    public async Task<Result<AttachmentDownload>> HandleAsync(
        Guid transactionId,
        Guid attachmentId,
        Guid userId,
        CancellationToken ct)
    {
        // Load the attachment and verify it belongs to the specified transaction
        var attachment = await db.TransactionAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TransactionId == transactionId, ct);

        if (attachment is null)
        {
            return Error.NotFound("Attachment not found.");
        }

        // Verify access through the transaction's wallet
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

        var role = await Wallets.WalletAccessChecker.GetRoleAsync(db, transaction.WalletId, userId, ct);
        if (role is null)
        {
            return Error.Forbidden("You do not have access to this attachment.");
        }

        var stream = await storage.DownloadFileAsync(
            Common.Utils.BucketNames.Attachments, attachment.ObjectName, ct);

        return new AttachmentDownload(stream, attachment.ContentType, attachment.FileName);
    }
}
