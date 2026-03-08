using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Recurring.UpdatePattern;

// Updates an existing recurring pattern for the authenticated user.
// StartDate is immutable after creation. If Frequency changed, NextOccurrence is recalculated
// from max(today, StartDate) using the new frequency.
public sealed class UpdatePatternHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdatePatternEndpoint.UpdatePatternResponse>> HandleAsync(
        Guid id,
        UpdatePatternEndpoint.UpdatePatternRequest request,
        Guid userId,
        CancellationToken ct)
    {
        var pattern = await db.RecurringPatterns
            .Include(r => r.Category)
            .Include(r => r.Wallet)
            .Include(r => r.DestinationWallet)
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null, ct);

        if (pattern is null)
        {
            return Error.NotFound("Recurring pattern not found.");
        }

        if (pattern.UserId != userId)
        {
            return Error.Forbidden("You do not have access to this recurring pattern.");
        }

        // Parse TransactionType (case-insensitive)
        if (!Enum.TryParse<TransactionType>(request.TransactionType, ignoreCase: true, out var transactionType))
        {
            return Error.Validation("Invalid transaction type. Must be Income, Expense, or Transfer.");
        }

        // Parse RecurrenceFrequency (case-insensitive)
        if (!Enum.TryParse<RecurrenceFrequency>(request.Frequency, ignoreCase: true, out var frequency))
        {
            return Error.Validation("Invalid frequency. Must be Daily, Weekly, Biweekly, Monthly, or Yearly.");
        }

        // Parse optional EndDate using NodaTimeParser
        LocalDate? endDate = null;
        if (request.EndDate is not null)
        {
            var endResult = NodaTimeParser.ParseLocalDate(request.EndDate, "EndDate");
            if (endResult.IsFailure) return endResult.Error;
            endDate = endResult.Value;

            // StartDate is immutable; validate EndDate against it
            if (endDate.Value < pattern.StartDate)
            {
                return Error.Validation("EndDate cannot be before StartDate.");
            }

            var today = clock.GetCurrentInstant().InUtc().Date;
            if (endDate.Value > today.PlusYears(10))
            {
                return Error.Validation("EndDate cannot be more than 10 years in the future.");
            }
        }

        // Verify source wallet access
        var sourceWallet = await GetAccessibleWalletAsync(request.WalletId, userId, ct);
        if (sourceWallet is null)
        {
            return Error.Forbidden("You do not have access to the specified wallet.");
        }

        // Transfer requires distinct destination wallet
        Wallet? destinationWallet = null;
        if (transactionType == TransactionType.Transfer)
        {
            if (request.DestinationWalletId is null)
            {
                return Error.Validation("Destination wallet is required for Transfer patterns.");
            }

            if (request.DestinationWalletId == request.WalletId)
            {
                return Error.Validation("Source and destination wallets must be different.");
            }

            destinationWallet = await GetAccessibleWalletAsync(request.DestinationWalletId.Value, userId, ct);
            if (destinationWallet is null)
            {
                return Error.Forbidden("You do not have access to the destination wallet.");
            }
        }

        // Verify category access
        var category = await db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == null || c.UserId == userId) &&
                c.DeletedAt == null, ct);

        if (category is null)
        {
            return Error.NotFound("Category not found or not accessible.");
        }

        // If frequency changed, recalculate NextOccurrence from max(today, StartDate)
        var now = clock.GetCurrentInstant().InUtc().Date;
        if (pattern.Frequency != frequency)
        {
            var baseDate = now > pattern.StartDate ? now : pattern.StartDate;
            pattern.NextOccurrence = RecurrenceCalculator.CalculateNext(baseDate, frequency);
        }

        pattern.Name = request.Name;
        pattern.Amount = request.Amount;
        pattern.Description = request.Description;
        pattern.TransactionType = transactionType;
        pattern.Frequency = frequency;
        pattern.CategoryId = request.CategoryId;
        pattern.WalletId = request.WalletId;
        pattern.DestinationWalletId = transactionType == TransactionType.Transfer ? request.DestinationWalletId : null;
        pattern.EndDate = endDate;
        pattern.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return new UpdatePatternEndpoint.UpdatePatternResponse(
            pattern.Id,
            pattern.Name,
            pattern.Amount,
            pattern.Description,
            pattern.TransactionType.ToString(),
            pattern.Frequency.ToString(),
            pattern.CategoryId,
            category.Name,
            pattern.WalletId,
            sourceWallet.Name,
            pattern.DestinationWalletId,
            destinationWallet?.Name,
            LocalDatePattern.Iso.Format(pattern.StartDate),
            endDate.HasValue ? LocalDatePattern.Iso.Format(endDate.Value) : null,
            LocalDatePattern.Iso.Format(pattern.NextOccurrence),
            pattern.IsActive,
            pattern.UpdatedAt);
    }

    // Returns the wallet if the user has at least Editor role; null otherwise.
    private async Task<Wallet?> GetAccessibleWalletAsync(Guid walletId, Guid userId, CancellationToken ct)
    {
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return null;
        }

        var role = await Wallets.WalletAccessChecker.GetRoleAsync(db, walletId, userId, ct);
        return role is not null && role.Value <= Domain.Entities.WalletMemberRole.Editor ? wallet : null;
    }
}
