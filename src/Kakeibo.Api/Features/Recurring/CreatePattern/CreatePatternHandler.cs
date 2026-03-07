using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Recurring.CreatePattern;

// Creates a new recurring transaction pattern for the authenticated user.
// Sets NextOccurrence to StartDate; the Hangfire job will advance it after the first generation.
public sealed class CreatePatternHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<CreatePatternEndpoint.CreatePatternResponse>> HandleAsync(
        CreatePatternEndpoint.CreatePatternRequest request,
        Guid userId,
        CancellationToken ct)
    {
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

        // Parse StartDate using NodaTime ISO pattern
        var startParseResult = LocalDatePattern.Iso.Parse(request.StartDate);
        if (!startParseResult.Success)
        {
            return Error.Validation("Invalid StartDate format. Expected ISO 8601 (YYYY-MM-DD).");
        }

        var startDate = startParseResult.Value;

        // Parse optional EndDate
        LocalDate? endDate = null;
        if (request.EndDate is not null)
        {
            var endParseResult = LocalDatePattern.Iso.Parse(request.EndDate);
            if (!endParseResult.Success)
            {
                return Error.Validation("Invalid EndDate format. Expected ISO 8601 (YYYY-MM-DD).");
            }

            endDate = endParseResult.Value;

            if (endDate.Value < startDate)
            {
                return Error.Validation("EndDate cannot be before StartDate.");
            }

            // Enforce 10-year maximum as per business constraints
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

        // Verify the category is accessible — system categories (UserId == null) are always accessible
        var category = await db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == null || c.UserId == userId) &&
                c.DeletedAt == null, ct);

        if (category is null)
        {
            return Error.NotFound("Category not found or not accessible.");
        }

        var pattern = new RecurringPattern
        {
            UserId = userId,
            Name = request.Name,
            Amount = request.Amount,
            Description = request.Description,
            TransactionType = transactionType,
            Frequency = frequency,
            CategoryId = request.CategoryId,
            WalletId = request.WalletId,
            DestinationWalletId = transactionType == TransactionType.Transfer ? request.DestinationWalletId : null,
            StartDate = startDate,
            EndDate = endDate,
            // Initial NextOccurrence is the StartDate; advanced after each generation
            NextOccurrence = startDate
        };

        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        return new CreatePatternEndpoint.CreatePatternResponse(
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
            pattern.CreatedAt);
    }

    // Returns the wallet if the user is the owner or an active WalletMember; null otherwise.
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
