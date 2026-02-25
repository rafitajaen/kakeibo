using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Transactions.RecordTransaction;

// Records a financial transaction (income, expense, or transfer) and atomically updates wallet balance(s).
public sealed class RecordTransactionHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<RecordTransactionEndpoint.RecordTransactionResponse>> HandleAsync(
        RecordTransactionEndpoint.RecordTransactionRequest request,
        Guid userId,
        CancellationToken ct)
    {
        // Parse TransactionType (case-insensitive)
        if (!Enum.TryParse<TransactionType>(request.Type, ignoreCase: true, out var transactionType))
            return Error.Validation("Invalid transaction type. Must be Income, Expense, or Transfer.");

        // Parse LocalDate using NodaTime ISO pattern — never DateTime.Parse
        var parseResult = LocalDatePattern.Iso.Parse(request.Date);
        if (!parseResult.Success)
            return Error.Validation("Invalid date format. Expected ISO 8601 (YYYY-MM-DD).");

        var date = parseResult.Value;

        // Reject dates more than 1 year in the future
        var today = clock.GetCurrentInstant().InUtc().Date;
        if (date > today.PlusYears(1))
            return Error.Validation("Transaction date cannot be more than 1 year in the future.");

        // Verify the user has access to the source wallet
        var sourceWallet = await GetAccessibleWalletAsync(request.WalletId, userId, ct);
        if (sourceWallet is null)
            return Error.Forbidden("You do not have access to the specified wallet.");

        // Transfer requires a non-null, distinct destination wallet
        if (transactionType == TransactionType.Transfer)
        {
            if (request.DestinationWalletId is null)
                return Error.Validation("Destination wallet is required for Transfer transactions.");

            if (request.DestinationWalletId == request.WalletId)
                return Error.Validation("Source and destination wallets must be different.");

            var destWallet = await GetAccessibleWalletAsync(request.DestinationWalletId.Value, userId, ct);
            if (destWallet is null)
                return Error.Forbidden("You do not have access to the destination wallet.");
        }

        // Verify the category is accessible — system categories (UserId == null) are always accessible
        var category = await db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == null || c.UserId == userId) &&
                c.DeletedAt == null, ct);

        if (category is null)
            return Error.NotFound("Category not found or not accessible.");

        var transaction = new Transaction
        {
            Type = transactionType,
            Amount = request.Amount,
            Description = request.Description,
            Date = date,
            CategoryId = request.CategoryId,
            WalletId = request.WalletId,
            DestinationWalletId = transactionType == TransactionType.Transfer
                ? request.DestinationWalletId
                : null,
            UserId = userId
        };

        db.Transactions.Add(transaction);

        // Apply balance delta atomically in the same SaveChangesAsync call
        var sourceBalance = await db.WalletBalances
            .FirstOrDefaultAsync(wb => wb.WalletId == request.WalletId, ct);

        if (sourceBalance is null)
            return Error.Internal("Wallet balance record not found.");

        switch (transactionType)
        {
            case TransactionType.Income:
                sourceBalance.Balance += request.Amount;
                break;

            case TransactionType.Expense:
                sourceBalance.Balance -= request.Amount;
                break;

            case TransactionType.Transfer:
                sourceBalance.Balance -= request.Amount;

                var destBalance = await db.WalletBalances
                    .FirstOrDefaultAsync(wb => wb.WalletId == request.DestinationWalletId!.Value, ct);

                if (destBalance is null)
                    return Error.Internal("Destination wallet balance record not found.");

                destBalance.Balance += request.Amount;
                destBalance.UpdatedAt = clock.GetCurrentInstant();
                break;
        }

        sourceBalance.UpdatedAt = clock.GetCurrentInstant();

        // Fire-and-forget event — consumed by Auditing and future Wallets/Budgets/Goals handlers
        eventBus.Publish(new TransactionRecordedEvent
        {
            TransactionId = transaction.Id,
            WalletId = transaction.WalletId,
            Type = transactionType.ToString(),
            Amount = request.Amount,
            UserId = userId,
            CategoryId = request.CategoryId,
            Date = date
        });

        await db.SaveChangesAsync(ct);

        return new RecordTransactionEndpoint.RecordTransactionResponse(
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

    // Returns the wallet if the user is the owner or an active WalletMember; null otherwise.
    private async Task<Wallet?> GetAccessibleWalletAsync(Guid walletId, Guid userId, CancellationToken ct)
    {
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
            return null;

        var isOwner = wallet.OwnerId == userId;
        var isMember = await db.WalletMembers
            .AnyAsync(m => m.WalletId == walletId && m.UserId == userId, ct);

        return isOwner || isMember ? wallet : null;
    }
}
