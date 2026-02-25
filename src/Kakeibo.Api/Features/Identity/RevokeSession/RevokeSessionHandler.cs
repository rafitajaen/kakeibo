using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.RevokeSession;

// Revokes a specific refresh token (session) owned by the authenticated user.
public sealed class RevokeSessionHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<RevokeSessionEndpoint.RevokeSessionResponse>> HandleAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken ct)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == sessionId, ct);

        if (token is null)
            return Error.NotFound("Session not found.");

        // Prevent users from revoking other users' sessions
        if (token.UserId != userId)
            return Error.Forbidden("You do not have access to this session.");

        if (token.RevokedAt is not null)
            return new RevokeSessionEndpoint.RevokeSessionResponse("Session already revoked.");

        var now = clock.GetCurrentInstant();
        token.RevokedAt = now;
        token.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return new RevokeSessionEndpoint.RevokeSessionResponse("Session revoked successfully.");
    }
}
