using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Budgets.GetBudgetStatus;

// Returns the real-time status of a budget with computed fields.
public sealed class GetBudgetStatusHandler(AppDbContext db)
{
    public async Task<Result<GetBudgetStatusEndpoint.GetBudgetStatusResponse>> HandleAsync(
        Guid budgetId,
        Guid userId,
        CancellationToken ct)
    {
        var budget = await db.Budgets
            .Include(b => b.Category)
            .Include(b => b.Wallet)
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.DeletedAt == null, ct);

        if (budget is null)
        {
            return Error.NotFound("Budget not found.");
        }

        if (budget.UserId != userId)
        {
            return Error.Forbidden("You do not have access to this budget.");
        }

        // Compute derived fields from current spending and limit
        var remaining = Math.Max(0m, budget.Limit - budget.CurrentSpending);
        var percentageUsed = budget.Limit > 0
            ? budget.CurrentSpending / budget.Limit * 100m
            : 0m;

        // Determine status thresholds: Warning at 75%, Exceeded at 100%
        var warningThreshold = budget.Limit * 0.75m;
        var status = budget.CurrentSpending >= budget.Limit
            ? "Exceeded"
            : budget.CurrentSpending >= warningThreshold
                ? "Warning"
                : "OnTrack";

        return new GetBudgetStatusEndpoint.GetBudgetStatusResponse(
            budget.Id,
            budget.Name,
            budget.CategoryId,
            budget.Category!.Name,
            budget.Limit,
            budget.CurrentSpending,
            remaining,
            percentageUsed,
            status,
            LocalDatePattern.Iso.Format(budget.StartDate),
            LocalDatePattern.Iso.Format(budget.EndDate),
            budget.WalletId,
            budget.Wallet?.Name);
    }
}
