using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Infrastructure.Storage;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Kakeibo.Api.Features.Transactions.DeleteAttachment;

// Deletes a transaction attachment: removes the file from MinIO and the metadata from the database.
public sealed class DeleteAttachmentHandler(
    AppDbContext db,
    IStorageService storage,
    ILogger<DeleteAttachmentHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(
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

        // Require at least Editor role to delete attachments
        var role = await Wallets.WalletAccessChecker.GetRoleAsync(db, transaction.WalletId, userId, ct);
        if (role is null || role.Value > Domain.Entities.WalletMemberRole.Editor)
        {
            return Error.Forbidden("You do not have access to this attachment.");
        }

        // Delete from MinIO first — if this fails, no DB changes are committed.
        // If the DB removal fails after MinIO succeeds, the metadata becomes orphaned (acceptable; tracked in tech debt).
        try
        {
            await storage.DeleteFileAsync(BucketNames.Attachments, attachment.ObjectName, ct);
        }
        catch (Exception ex)
        {
            logger.AttachmentStorageDeletionFailed(transactionId, attachmentId, ex);
            return Error.Internal("Failed to delete attachment file. Please try again.");
        }

        db.TransactionAttachments.Remove(attachment);
        await db.SaveChangesAsync(ct);

        logger.AttachmentDeleted(transactionId, attachmentId, userId);

        return true;
    }
}
