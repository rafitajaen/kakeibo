using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Wallets.ListWallets;

// Returns wallets where the user is a WalletMember (any role).
public sealed class ListWalletsHandler(AppDbContext db)
{
    public async Task<IReadOnlyList<ListWalletsEndpoint.ListWalletsResponse>> HandleAsync(
        Guid userId,
        bool includeArchived,
        CancellationToken ct)
    {
        // All wallet access is now through WalletMember records
        var query = db.Wallets
            .AsNoTracking()
            .Where(w => w.WalletMembers.Any(m => m.UserId == userId));

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
                Icon: w.Icon,
                BackgroundColor: w.BackgroundColor,
                TextColor: w.TextColor,
                w.CreatedAt))
            .ToListAsync(ct);

        return wallets;
    }
}
