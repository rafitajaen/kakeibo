using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Auth;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.LoginUser;

// Authenticates a user and issues JWT access and refresh tokens via HttpOnly cookies.
public sealed class LoginUserHandler(
    AppDbContext db,
    JwtService jwtService,
    TokenCookieService tokenCookieService,
    IEventBus eventBus,
    IOptions<JwtOptions> jwtOptions,
    IClock clock)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<LoginUserEndpoint.LoginUserResponse>> HandleAsync(
        LoginUserEndpoint.LoginUserRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        // Use constant-time comparison via VerifyPassword to prevent timing attacks.
        // PasswordHash is nullable (Google-only accounts have no password).
        if (user is null || user.PasswordHash is null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Error.Unauthorized("Invalid email or password.");
        }

        if (!user.IsVerified)
        {
            return Error.Validation("Email address has not been verified. Please check your inbox.");
        }

        // Blocked users cannot authenticate regardless of credentials
        if (user.IsBlocked)
        {
            return Error.Forbidden("This account has been suspended. Please contact support.");
        }

        var now = clock.GetCurrentInstant();

        // If the user had previously requested deletion, cancel it (account recovery within grace period)
        if (user.DeletionRequestedAt is not null)
        {
            user.DeletionRequestedAt = null;
            user.UpdatedAt = now;
        }

        // Generate tokens
        var accessToken = jwtService.GenerateAccessToken(user);
        var rawRefreshToken = RandomString.Generate(64, CharSets.Alphanumeric);
        var refreshTokenHash = TokenHasher.Hash(rawRefreshToken);

        // Use fully-qualified type to avoid namespace collision with Features.Identity.RefreshToken folder
        var refreshToken = new Domain.Entities.RefreshToken
        {
            TokenHash = refreshTokenHash,
            UserId = user.Id,
            ExpiresAt = now.Plus(Duration.FromDays(_jwtOptions.RefreshTokenDays)),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        db.RefreshTokens.Add(refreshToken);

        eventBus.Publish(new UserLoggedInEvent
        {
            UserId = user.Id,
            IpAddress = refreshToken.IpAddress,
            UserAgent = refreshToken.UserAgent
        });

        await db.SaveChangesAsync(ct);

        // Set HttpOnly cookies — tokens never exposed to JavaScript
        tokenCookieService.SetTokenCookies(httpContext, accessToken, rawRefreshToken);

        return new LoginUserEndpoint.LoginUserResponse(
            user.Id, user.Email, user.Role.ToString(), user.Currency, user.Username);
    }
}
