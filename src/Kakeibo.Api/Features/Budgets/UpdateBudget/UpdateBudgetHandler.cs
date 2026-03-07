using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Budgets.UpdateBudget;

// Updates budget fields. If CategoryId, WalletId, StartDate, or EndDate changes,
// CurrentSpending is recalculated from existing expense transactions.
public sealed class UpdateBudgetHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdateBudgetEndpoint.UpdateBudgetResponse>> HandleAsync(
        Guid budgetId,
        UpdateBudgetEndpoint.UpdateBudgetRequest request,
        Guid userId,
        CancellationToken ct)
    {
        var budget = await db.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.DeletedAt == null, ct);

        if (budget is null)
        {
            return Error.NotFound("Budget not found.");
        }

        if (budget.UserId != userId)
        {
            return Error.Forbidden("You do not have access to this budget.");
        }

        // Parse new dates
        var startParseResult = LocalDatePattern.Iso.Parse(request.StartDate);
        if (!startParseResult.Success)
        {
            return Error.Validation("Invalid StartDate format. Expected ISO 8601 (YYYY-MM-DD).");
        }

        var endParseResult = LocalDatePattern.Iso.Parse(request.EndDate);
        if (!endParseResult.Success)
        {
            return Error.Validation("Invalid EndDate format. Expected ISO 8601 (YYYY-MM-DD).");
        }

        var newStartDate = startParseResult.Value;
        var newEndDate = endParseResult.Value;

        if (newEndDate < newStartDate)
        {
            return Error.Validation("EndDate must be on or after StartDate.");
        }

        if (newEndDate > newStartDate.PlusYears(5))
        {
            return Error.Validation("Budget period cannot exceed 5 years.");
        }

        // Verify category accessibility
        var category = await db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == null || c.UserId == userId) &&
                c.DeletedAt == null, ct);

        if (category is null)
        {
            return Error.NotFound("Category not found or not accessible.");
        }

        // Verify wallet access if specified
        string? walletName = null;
        if (request.WalletId.HasValue)
        {
            var wallet = await db.Wallets
                .FirstOrDefaultAsync(w => w.Id == request.WalletId.Value && w.DeletedAt == null, ct);

            if (wallet is null)
            {
                return Error.Forbidden("You do not have access to the specified wallet.");
            }

            var walletRole = await Wallets.WalletAccessChecker.GetRoleAsync(db, request.WalletId.Value, userId, ct);
            if (walletRole is null)
            {
                return Error.Forbidden("You do not have access to the specified wallet.");
            }

            walletName = wallet.Name;
        }

        // Detect if any dimension affecting spending calculation has changed
        var needsRecalculation =
            budget.CategoryId != request.CategoryId ||
            budget.WalletId != request.WalletId ||
            budget.StartDate != newStartDate ||
            budget.EndDate != newEndDate;

        // Update mutable fields
        budget.Name = request.Name;
        budget.CategoryId = request.CategoryId;
        budget.Limit = request.Limit;
        budget.StartDate = newStartDate;
        budget.EndDate = newEndDate;
        budget.WalletId = request.WalletId;
        budget.UpdatedAt = clock.GetCurrentInstant();

        // Recalculate spending from DB if dimensions changed — ensures consistency
        if (needsRecalculation)
        {
            budget.CurrentSpending = await db.Transactions
                .Where(t =>
                    t.UserId == userId &&
                    t.Type == TransactionType.Expense &&
                    t.CategoryId == budget.CategoryId &&
                    t.Date >= budget.StartDate &&
                    t.Date <= budget.EndDate &&
                    (budget.WalletId == null || t.WalletId == budget.WalletId) &&
                    t.DeletedAt == null)
                .SumAsync(t => t.Amount, ct);
        }

        await db.SaveChangesAsync(ct);

        return new UpdateBudgetEndpoint.UpdateBudgetResponse(
            budget.Id,
            budget.Name,
            budget.CategoryId,
            category.Name,
            budget.Limit,
            LocalDatePattern.Iso.Format(budget.StartDate),
            LocalDatePattern.Iso.Format(budget.EndDate),
            budget.WalletId,
            walletName,
            budget.CurrentSpending,
            budget.CreatedAt);
    }
}
