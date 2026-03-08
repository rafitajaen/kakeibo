using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Transactions.Categories.ListCategories;

// Returns system categories (always) and the caller's custom categories (filtered by archived state).
// When walletId is provided, also returns categories from all other wallet members (Strategy 3: shared pool).
public sealed class ListCategoriesHandler(AppDbContext db)
{
    public async Task<Result<IReadOnlyList<ListCategoriesEndpoint.ListCategoriesResponse>>> HandleAsync(
        Guid userId,
        bool includeArchived,
        Guid? walletId,
        CancellationToken ct)
    {
        // When walletId is provided, verify the caller is a member of the wallet.
        if (walletId.HasValue)
        {
            var isMember = await db.WalletMembers
                .AsNoTracking()
                .AnyAsync(wm => wm.WalletId == walletId.Value && wm.UserId == userId && wm.DeletedAt == null, ct);

            if (!isMember)
                return Error.Forbidden("You are not a member of this wallet.");
        }

        // Collect user IDs whose categories should be included.
        // Always includes the caller; when a wallet is provided, also includes all co-members.
        var memberUserIds = walletId.HasValue
            ? await db.WalletMembers
                .AsNoTracking()
                .Where(wm => wm.WalletId == walletId.Value && wm.DeletedAt == null)
                .Select(wm => wm.UserId)
                .ToListAsync(ct)
            : [userId];

        // System categories are always returned; custom categories are filtered by the resolved user pool.
        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => c.UserId == null
                || memberUserIds.Contains(c.UserId.Value) && (includeArchived || c.DeletedAt == null))
            .OrderBy(c => c.UserId == null ? 0 : 1) // system first, then custom
            .ThenBy(c => c.Name)
            .Select(c => new ListCategoriesEndpoint.ListCategoriesResponse(
                c.Id,
                c.Name,
                c.UserId == null,      // IsSystem
                c.DeletedAt != null,   // IsArchived
                c.IsPrivate,
                c.BackgroundColor,
                c.TextColor,
                c.Icon))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ListCategoriesEndpoint.ListCategoriesResponse>>.Success(categories);
    }
}
