using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Identity.ListSessions;

// Returns all active (non-revoked, non-expired) sessions for the authenticated user.
public sealed class ListSessionsHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<ListSessionsEndpoint.ListSessionsResponse>> HandleAsync(
        Guid userId,
        CancellationToken ct)
    {
        var now = clock.GetCurrentInstant();

        var sessions = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => new ListSessionsEndpoint.SessionItem(
                rt.Id,
                rt.IpAddress,
                rt.UserAgent,
                InstantPattern.General.Format(rt.CreatedAt)))
            .ToListAsync(ct);

        return new ListSessionsEndpoint.ListSessionsResponse(sessions);
    }
}
