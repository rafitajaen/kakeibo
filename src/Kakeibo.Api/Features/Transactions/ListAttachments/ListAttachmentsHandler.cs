using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Transactions.ListAttachments;

// Returns all attachments for a transaction the user has access to, ordered by creation date.
public sealed class ListAttachmentsHandler(AppDbContext db)
{
    public async Task<Result<ListAttachmentsEndpoint.ListAttachmentsResponse>> HandleAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken ct)
    {
        // Verify the transaction exists and is accessible
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
            return Error.Forbidden("You do not have access to this transaction.");
        }

        var items = await db.TransactionAttachments
            .Where(a => a.TransactionId == transactionId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new ListAttachmentsEndpoint.AttachmentItem(
                a.Id,
                a.TransactionId,
                a.FileName,
                a.ContentType,
                a.FileSizeBytes,
                a.UploadedByUserId,
                a.CreatedAt))
            .ToListAsync(ct);

        return new ListAttachmentsEndpoint.ListAttachmentsResponse(items);
    }
}
