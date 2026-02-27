using System.Security.Claims;
using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.LogoutAllSessions;

// Revokes all active refresh tokens for the current user, ending all sessions.
public sealed class LogoutAllSessionsHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<LogoutAllSessionsEndpoint.LogoutAllSessionsResponse>> HandleAsync(
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Error.Unauthorized("User is not authenticated.");
        }

        var now = clock.GetCurrentInstant();

        // Revoke all active (non-revoked) refresh tokens for this user
        var activeTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);

        eventBus.Publish(new UserLoggedOutEvent
        {
            UserId = userId,
            AllSessions = true
        });

        // Clear current session cookies
        httpContext.Response.Cookies.Delete("access_token");
        httpContext.Response.Cookies.Delete("refresh_token");

        return new LogoutAllSessionsEndpoint.LogoutAllSessionsResponse(
            "All sessions revoked successfully.",
            activeTokens.Count);
    }
}
