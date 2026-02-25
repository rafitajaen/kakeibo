using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Transactions.UpdateTransaction;

// Updates a transaction's amount, description, date, category, and destination wallet.
// Type and WalletId are immutable — simplifies balance recalculation significantly.
public sealed class UpdateTransactionHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<UpdateTransactionEndpoint.UpdateTransactionResponse>> HandleAsync(
        Guid transactionId,
        UpdateTransactionEndpoint.UpdateTransactionRequest request,
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

        // Parse new date
        var parseResult = LocalDatePattern.Iso.Parse(request.Date);
        if (!parseResult.Success)
            return Error.Validation("Invalid date format. Expected ISO 8601 (YYYY-MM-DD).");

        var newDate = parseResult.Value;
        var today = clock.GetCurrentInstant().InUtc().Date;
        if (newDate > today.PlusYears(1))
            return Error.Validation("Transaction date cannot be more than 1 year in the future.");

        // Verify category accessibility
        var category = await db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == null || c.UserId == userId) &&
                c.DeletedAt == null, ct);

        if (category is null)
            return Error.NotFound("Category not found or not accessible.");

        // For Transfer: if destination is changing, verify access to new destination
        var oldDestinationWalletId = transaction.DestinationWalletId;
        var newDestinationWalletId = transaction.Type == TransactionType.Transfer
            ? request.DestinationWalletId
            : null;

        if (transaction.Type == TransactionType.Transfer && newDestinationWalletId != oldDestinationWalletId)
        {
            if (newDestinationWalletId is null)
                return Error.Validation("Destination wallet is required for Transfer transactions.");

            if (newDestinationWalletId == transaction.WalletId)
                return Error.Validation("Source and destination wallets must be different.");

            var newDestWallet = await db.Wallets
                .FirstOrDefaultAsync(w => w.Id == newDestinationWalletId && w.DeletedAt == null, ct);

            if (newDestWallet is null)
                return Error.NotFound("Destination wallet not found.");

            var newDestIsOwner = newDestWallet.OwnerId == userId;
            var newDestIsMember = await db.WalletMembers
                .AnyAsync(m => m.WalletId == newDestinationWalletId && m.UserId == userId, ct);

            if (!newDestIsOwner && !newDestIsMember)
                return Error.Forbidden("You do not have access to the destination wallet.");
        }

        var oldAmount = transaction.Amount;
        var newAmount = request.Amount;

        // Apply balance delta — all wallets loaded before any mutation (atomicity)
        var sourceBalance = await db.WalletBalances
            .FirstOrDefaultAsync(wb => wb.WalletId == transaction.WalletId, ct);

        if (sourceBalance is null)
            return Error.Internal("Wallet balance record not found.");

        switch (transaction.Type)
        {
            case TransactionType.Income:
                // New balance = current + (newAmount - oldAmount)
                sourceBalance.Balance += newAmount - oldAmount;
                break;

            case TransactionType.Expense:
                // New balance = current - (newAmount - oldAmount)
                sourceBalance.Balance -= newAmount - oldAmount;
                break;

            case TransactionType.Transfer:
                if (newDestinationWalletId == oldDestinationWalletId)
                {
                    // Same destination: shift delta on both sides
                    var delta = newAmount - oldAmount;
                    sourceBalance.Balance -= delta;

                    var destBalance = await db.WalletBalances
                        .FirstOrDefaultAsync(wb => wb.WalletId == oldDestinationWalletId!.Value, ct);
                    if (destBalance is null)
                        return Error.Internal("Destination wallet balance record not found.");

                    destBalance.Balance += delta;
                    destBalance.UpdatedAt = clock.GetCurrentInstant();
                }
                else
                {
                    // Destination changed: revert old impact, apply new
                    sourceBalance.Balance -= newAmount - oldAmount;

                    var oldDestBalance = await db.WalletBalances
                        .FirstOrDefaultAsync(wb => wb.WalletId == oldDestinationWalletId!.Value, ct);
                    if (oldDestBalance is null)
                        return Error.Internal("Old destination wallet balance record not found.");

                    // Revert the amount that was credited to old destination
                    oldDestBalance.Balance -= oldAmount;
                    oldDestBalance.UpdatedAt = clock.GetCurrentInstant();

                    var newDestBalance = await db.WalletBalances
                        .FirstOrDefaultAsync(wb => wb.WalletId == newDestinationWalletId!.Value, ct);
                    if (newDestBalance is null)
                        return Error.Internal("New destination wallet balance record not found.");

                    // Credit new destination
                    newDestBalance.Balance += newAmount;
                    newDestBalance.UpdatedAt = clock.GetCurrentInstant();
                }
                break;
        }

        sourceBalance.UpdatedAt = clock.GetCurrentInstant();

        // Capture old values before mutation — needed for budget recalculation in event handlers
        var oldCategoryId = transaction.CategoryId;
        var oldDate = transaction.Date;

        // Update the transaction fields
        transaction.Amount = request.Amount;
        transaction.Description = request.Description;
        transaction.Date = newDate;
        transaction.CategoryId = request.CategoryId;
        transaction.DestinationWalletId = newDestinationWalletId;
        transaction.UpdatedAt = clock.GetCurrentInstant();

        eventBus.Publish(new TransactionUpdatedEvent
        {
            TransactionId = transaction.Id,
            WalletId = transaction.WalletId,
            Type = transaction.Type.ToString(),
            Amount = request.Amount,
            UserId = userId,
            CategoryId = request.CategoryId,
            Date = newDate,
            OldCategoryId = oldCategoryId,
            OldDate = oldDate,
            OldAmount = oldAmount
        });

        await db.SaveChangesAsync(ct);

        return new UpdateTransactionEndpoint.UpdateTransactionResponse(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.Amount,
            transaction.Description,
            LocalDatePattern.Iso.Format(transaction.Date),
            transaction.CategoryId,
            transaction.WalletId,
            transaction.DestinationWalletId,
            transaction.UserId,
            transaction.CreatedAt);
    }
}
