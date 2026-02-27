using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Wallets.ListWallets;

// Returns wallets where the user is either the owner or a WalletMember.
public sealed class ListWalletsHandler(AppDbContext db)
{
    public async Task<IReadOnlyList<ListWalletsEndpoint.ListWalletsResponse>> HandleAsync(
        Guid userId,
        bool includeArchived,
        CancellationToken ct)
    {
        // Include wallets owned by the user OR where the user is a WalletMember
        var query = db.Wallets.Where(w =>
            w.OwnerId == userId ||
            w.WalletMembers.Any(m => m.UserId == userId));

        // Exclude archived wallets unless explicitly requested
        if (!includeArchived)
        {
            query = query.Where(w => w.DeletedAt == null);
        }

        var wallets = await query
            .OrderBy(w => w.CreatedAt)
            .Select(w => new ListWalletsEndpoint.ListWalletsResponse(
                w.Id,
                w.Name,
                w.Type.ToString(),
                w.Currency,
                Balance: w.WalletBalance != null ? w.WalletBalance.Balance : 0m,
                IsArchived: w.DeletedAt != null,
                w.CreatedAt))
            .ToListAsync(ct);

        return wallets;
    }
}
