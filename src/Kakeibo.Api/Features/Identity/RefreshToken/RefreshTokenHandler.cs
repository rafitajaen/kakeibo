using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Infrastructure.Auth;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.RefreshToken;

// Rotates the refresh token and issues a new access token if the current refresh token is valid.
public sealed class RefreshTokenHandler(
    AppDbContext db,
    JwtService jwtService,
    TokenCookieService tokenCookieService,
    IOptions<JwtOptions> jwtOptions,
    IClock clock)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<RefreshTokenEndpoint.RefreshTokenResponse>> HandleAsync(
        HttpContext httpContext,
        CancellationToken ct)
    {
        var rawToken = httpContext.Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(rawToken))
        {
            return Error.Unauthorized("Refresh token not found.");
        }

        var tokenHash = TokenHasher.Hash(rawToken);
        var now = clock.GetCurrentInstant();

        var storedToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        // Validate token exists, is not revoked, and is not expired
        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt <= now)
        {
            return Error.Unauthorized("Invalid or expired refresh token.");
        }

        if (storedToken.User is null)
        {
            return Error.Unauthorized("Associated user not found.");
        }

        // Revoke old token — one-time use, rotate to new one
        storedToken.RevokedAt = now;
        storedToken.UpdatedAt = now;

        var newRawToken = RandomString.Generate(64, CharSets.Alphanumeric);
        var newTokenHash = TokenHasher.Hash(newRawToken);

        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            TokenHash = newTokenHash,
            UserId = storedToken.UserId,
            ExpiresAt = now.Plus(Duration.FromDays(_jwtOptions.RefreshTokenDays)),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        db.RefreshTokens.Add(newRefreshToken);

        var accessToken = jwtService.GenerateAccessToken(storedToken.User);

        await db.SaveChangesAsync(ct);

        // Set new cookies
        tokenCookieService.SetTokenCookies(httpContext, accessToken, newRawToken);

        return new RefreshTokenEndpoint.RefreshTokenResponse("Token refreshed successfully.");
    }
}
