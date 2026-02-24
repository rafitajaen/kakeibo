using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Security.Claims;

namespace Kakeibo.Api.Features.Identity.LogoutUser;

// Revokes the current refresh token and clears the auth cookies.
public sealed class LogoutUserHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<LogoutUserEndpoint.LogoutUserResponse>> HandleAsync(
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);

        var rawToken = httpContext.Request.Cookies["refresh_token"];
        if (!string.IsNullOrEmpty(rawToken))
        {
            var tokenHash = TokenHasher.Hash(rawToken);
            var now = clock.GetCurrentInstant();

            var storedToken = await db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null, ct);

            if (storedToken is not null)
            {
                storedToken.RevokedAt = now;
                storedToken.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
            }
        }

        if (userId.HasValue)
        {
            eventBus.Publish(new UserLoggedOutEvent
            {
                UserId = userId.Value,
                AllSessions = false
            });
        }

        ClearAuthCookies(httpContext);

        return new LogoutUserEndpoint.LogoutUserResponse("Logged out successfully.");
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    // Removes access and refresh token cookies from the response.
    private static void ClearAuthCookies(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete("access_token");
        httpContext.Response.Cookies.Delete("refresh_token");
    }
}
