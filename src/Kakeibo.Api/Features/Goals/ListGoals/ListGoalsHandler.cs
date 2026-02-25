using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Goals.ListGoals;

// Lists all non-deleted goals for the authenticated user, ordered by creation date descending.
public sealed class ListGoalsHandler(AppDbContext db)
{
    public async Task<List<ListGoalsEndpoint.ListGoalsResponse>> HandleAsync(
        Guid userId,
        Guid? walletId,
        CancellationToken ct)
    {
        var query = db.Goals
            .Include(g => g.Wallet)
            .Where(g => g.UserId == userId && g.DeletedAt == null);

        // Apply optional wallet filter
        if (walletId.HasValue)
            query = query.Where(g => g.WalletId == walletId.Value);

        var goals = await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);

        return goals.Select(g => new ListGoalsEndpoint.ListGoalsResponse(
            g.Id,
            g.Name,
            g.TargetAmount,
            g.Deadline.HasValue ? LocalDatePattern.Iso.Format(g.Deadline.Value) : null,
            g.WalletId,
            g.Wallet!.Name,
            g.CurrentProgress,
            g.LastMilestone,
            g.CreatedAt)).ToList();
    }
}
