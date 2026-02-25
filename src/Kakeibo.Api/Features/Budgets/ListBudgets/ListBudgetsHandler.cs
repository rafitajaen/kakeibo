using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Budgets.ListBudgets;

// Lists all non-deleted budgets for the authenticated user, ordered by creation date descending.
public sealed class ListBudgetsHandler(AppDbContext db)
{
    public async Task<List<ListBudgetsEndpoint.ListBudgetsResponse>> HandleAsync(
        Guid userId,
        Guid? categoryId,
        Guid? walletId,
        CancellationToken ct)
    {
        var query = db.Budgets
            .Include(b => b.Category)
            .Include(b => b.Wallet)
            .Where(b => b.UserId == userId && b.DeletedAt == null);

        // Apply optional filters
        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryId == categoryId.Value);

        if (walletId.HasValue)
            query = query.Where(b => b.WalletId == walletId.Value);

        var budgets = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        return budgets.Select(b => new ListBudgetsEndpoint.ListBudgetsResponse(
            b.Id,
            b.Name,
            b.CategoryId,
            b.Category!.Name,
            b.Limit,
            LocalDatePattern.Iso.Format(b.StartDate),
            LocalDatePattern.Iso.Format(b.EndDate),
            b.WalletId,
            b.Wallet?.Name,
            b.CurrentSpending,
            b.CreatedAt)).ToList();
    }
}
