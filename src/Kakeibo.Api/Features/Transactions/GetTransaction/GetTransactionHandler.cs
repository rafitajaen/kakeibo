using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Transactions.GetTransaction;

// Returns a single transaction the user has access to (via wallet ownership or membership).
public sealed class GetTransactionHandler(AppDbContext db)
{
    public async Task<Result<GetTransactionEndpoint.GetTransactionResponse>> HandleAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken ct)
    {
        var transaction = await db.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.DeletedAt == null, ct);

        if (transaction is null)
        {
            return Error.NotFound("Transaction not found.");
        }

        // Verify access through the source wallet
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == transaction.WalletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.NotFound("Transaction not found.");
        }

        var role = await Wallets.WalletAccessChecker.GetRoleAsync(db, transaction.WalletId, userId, ct);
        var isMember = role is not null;

        // Non-members can only access transactions in public wallets
        if (!isMember && wallet.Visibility != Domain.Entities.WalletVisibility.Public)
        {
            return Error.Forbidden("You do not have access to this transaction.");
        }

        // Apply privacy masking for non-members when category is private
        var isPrivateCategory = transaction.Category!.IsPrivate;
        var categoryName = (!isMember && isPrivateCategory) ? "Private" : transaction.Category.Name;
        var description = (!isMember && isPrivateCategory) ? null : transaction.Description;

        return new GetTransactionEndpoint.GetTransactionResponse(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.Amount,
            description,
            LocalDatePattern.Iso.Format(transaction.Date),
            transaction.CategoryId,
            categoryName,
            transaction.WalletId,
            transaction.DestinationWalletId,
            transaction.UserId,
            transaction.Notes,
            transaction.CreatedAt);
    }
}
