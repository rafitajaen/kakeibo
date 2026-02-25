using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Recurring.ListPatterns;

// Returns all non-deleted recurring patterns for the given user, ordered by next occurrence ascending.
public sealed class ListPatternsHandler(AppDbContext db)
{
    public async Task<List<ListPatternsEndpoint.ListPatternsResponse>> HandleAsync(
        Guid userId,
        CancellationToken ct)
    {
        var patterns = await db.RecurringPatterns
            .Include(r => r.Category)
            .Include(r => r.Wallet)
            .Include(r => r.DestinationWallet)
            .Where(r => r.UserId == userId && r.DeletedAt == null)
            .OrderBy(r => r.NextOccurrence)
            .ToListAsync(ct);

        return patterns.Select(r => new ListPatternsEndpoint.ListPatternsResponse(
            r.Id,
            r.Name,
            r.Amount,
            r.Description,
            r.TransactionType.ToString(),
            r.Frequency.ToString(),
            r.CategoryId,
            r.Category!.Name,
            r.WalletId,
            r.Wallet!.Name,
            r.DestinationWalletId,
            r.DestinationWallet?.Name,
            LocalDatePattern.Iso.Format(r.StartDate),
            r.EndDate.HasValue ? LocalDatePattern.Iso.Format(r.EndDate.Value) : null,
            LocalDatePattern.Iso.Format(r.NextOccurrence),
            r.IsActive,
            r.CreatedAt)).ToList();
    }
}
