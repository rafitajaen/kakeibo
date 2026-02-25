using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.DeleteTransaction;

// Soft-deletes a transaction and reverses its balance impact atomically.
public sealed class DeleteTransactionHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken ct)
    {
        var transaction = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.DeletedAt == null, ct);

        if (transaction is null)
            return Error.NotFound("Transaction not found.");

        // Verify access through the source wallet
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == transaction.WalletId && w.DeletedAt == null, ct);

        if (wallet is null)
            return Error.NotFound("Transaction not found.");

        var isOwner = wallet.OwnerId == userId;
        var isMember = await db.WalletMembers
            .AnyAsync(m => m.WalletId == transaction.WalletId && m.UserId == userId, ct);

        if (!isOwner && !isMember)
            return Error.Forbidden("You do not have access to this transaction.");

        // Reverse the balance impact atomically
        var sourceBalance = await db.WalletBalances
            .FirstOrDefaultAsync(wb => wb.WalletId == transaction.WalletId, ct);

        if (sourceBalance is null)
            return Error.Internal("Wallet balance record not found.");

        switch (transaction.Type)
        {
            case TransactionType.Income:
                // Undo: income had increased balance, so subtract it back
                sourceBalance.Balance -= transaction.Amount;
                break;

            case TransactionType.Expense:
                // Undo: expense had decreased balance, so add it back
                sourceBalance.Balance += transaction.Amount;
                break;

            case TransactionType.Transfer:
                // Undo: source had amount deducted, destination had amount credited
                sourceBalance.Balance += transaction.Amount;

                var destBalance = await db.WalletBalances
                    .FirstOrDefaultAsync(wb => wb.WalletId == transaction.DestinationWalletId!.Value, ct);

                if (destBalance is null)
                    return Error.Internal("Destination wallet balance record not found.");

                destBalance.Balance -= transaction.Amount;
                destBalance.UpdatedAt = clock.GetCurrentInstant();
                break;
        }

        sourceBalance.UpdatedAt = clock.GetCurrentInstant();

        // Soft-delete: preserve audit trail, balances are already reversed above
        transaction.DeletedAt = clock.GetCurrentInstant();
        transaction.UpdatedAt = clock.GetCurrentInstant();

        eventBus.Publish(new TransactionDeletedEvent
        {
            TransactionId = transaction.Id,
            WalletId = transaction.WalletId,
            Type = transaction.Type.ToString(),
            Amount = transaction.Amount,
            UserId = userId,
            CategoryId = transaction.CategoryId,
            Date = transaction.Date
        });

        await db.SaveChangesAsync(ct);

        return true;
    }
}
