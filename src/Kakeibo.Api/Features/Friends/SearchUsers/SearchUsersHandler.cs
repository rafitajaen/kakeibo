using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Friends.SearchUsers;

// Searches users by partial username match. Excludes the requesting user. Max 20 results.
public sealed class SearchUsersHandler(AppDbContext db)
{
    private const int MaxResults = 20;

    public async Task<List<SearchUsersEndpoint.SearchUsersResponse>> HandleAsync(
        string query,
        Guid excludeUserId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return [];
        }

        var normalizedQuery = query.ToLowerInvariant();

        return await db.Users
            .AsNoTracking()
            .Where(u => u.Id != excludeUserId && u.Username.Contains(normalizedQuery))
            .OrderBy(u => u.Username)
            .Take(MaxResults)
            .Select(u => new SearchUsersEndpoint.SearchUsersResponse(
                u.Id,
                u.Username,
                u.Name,
                u.AvatarUrl))
            .ToListAsync(ct);
    }
}
