using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Admin.ListAdminUsers;

// Returns a paginated list of all platform users for admin management.
public sealed class ListAdminUsersHandler(AppDbContext db)
{
    private const int DefaultPageSize = 50;

    public async Task<IReadOnlyList<ListAdminUsersEndpoint.ListAdminUsersResponse>> HandleAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct)
    {
        var clampedSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, DefaultPageSize);
        var offset = (Math.Max(page, 1) - 1) * clampedSize;

        var query = db.Users.AsNoTracking();

        // Apply optional search filter by email or username
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.ToLowerInvariant();
            query = query.Where(u =>
                u.Email.Contains(normalized) ||
                u.Username.Contains(normalized));
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(offset)
            .Take(clampedSize)
            .Select(u => new ListAdminUsersEndpoint.ListAdminUsersResponse(
                u.Id,
                u.Email,
                u.Username,
                u.Name,
                u.Role.ToString(),
                u.IsVerified,
                u.IsBlocked,
                u.PasswordHash != null,
                u.GoogleId != null,
                u.CreatedAt))
            .ToListAsync(ct);
    }
}
