using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Budgets.DeleteBudget;

// Soft-deletes a budget, preserving audit trail.
public sealed class DeleteBudgetHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(
        Guid budgetId,
        Guid userId,
        CancellationToken ct)
    {
        var budget = await db.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.DeletedAt == null, ct);

        if (budget is null)
            return Error.NotFound("Budget not found.");

        if (budget.UserId != userId)
            return Error.Forbidden("You do not have access to this budget.");

        budget.DeletedAt = clock.GetCurrentInstant();
        budget.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return true;
    }
}
