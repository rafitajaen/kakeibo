using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Transactions.Categories.ListCategories;

// Returns system categories (always) and the caller's custom categories (filtered by archived state).
public sealed class ListCategoriesHandler(AppDbContext db)
{
    public async Task<IReadOnlyList<ListCategoriesEndpoint.ListCategoriesResponse>> HandleAsync(
        Guid userId,
        bool includeArchived,
        CancellationToken ct)
    {
        // System categories are always returned; custom categories are filtered by UserId and archived state.
        var categories = await db.Categories
            .Where(c => c.UserId == null || c.UserId == userId && (includeArchived || c.DeletedAt == null))
            .OrderBy(c => c.UserId == null ? 0 : 1) // system first, then custom
            .ThenBy(c => c.Name)
            .Select(c => new ListCategoriesEndpoint.ListCategoriesResponse(
                c.Id,
                c.Name,
                c.UserId == null,      // IsSystem
                c.DeletedAt != null))  // IsArchived
            .ToListAsync(ct);

        return categories;
    }
}
