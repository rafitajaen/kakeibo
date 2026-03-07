using Google.Apis.Auth;
using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Auth;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.GoogleLogin;

// Authenticates or registers a user via Google OAuth ID token.
// Three flows: (1) existing user by GoogleId → login, (2) existing user by email → link GoogleId,
// (3) new user → create account (verified, no password).
public sealed class GoogleLoginHandler(
    AppDbContext db,
    JwtService jwtService,
    TokenCookieService tokenCookieService,
    IEventBus eventBus,
    IOptions<GoogleAuthOptions> googleOptions,
    IOptions<JwtOptions> jwtOptions,
    IClock clock)
{
    private readonly GoogleAuthOptions _googleOptions = googleOptions.Value;
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<GoogleLoginEndpoint.GoogleLoginResponse>> HandleAsync(
        GoogleLoginEndpoint.GoogleLoginRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        // Validate the Google ID token
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_googleOptions.ClientId]
                });
        }
        catch (InvalidJwtException)
        {
            return Error.Unauthorized("Invalid Google ID token.");
        }

        var googleId = payload.Subject;
        var email = payload.Email.ToLowerInvariant();
        var now = clock.GetCurrentInstant();
        var isNewAccount = false;

        // Flow 1: Find existing user by GoogleId
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);

        if (user is null)
        {
            // Flow 2: Find existing user by email → link GoogleId
            user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

            if (user is not null)
            {
                // Link Google account to existing user
                user.GoogleId = googleId;
                user.UpdatedAt = now;

                // Mark email as verified if not already (Google has verified it)
                if (!user.IsVerified)
                {
                    user.IsVerified = true;
                    user.VerifiedAt = now;
                    user.EmailVerificationToken = null;
                    user.EmailVerificationTokenExpiresAt = null;
                }
            }
            else
            {
                // Flow 3: Create new user — no password, verified by Google
                user = new User
                {
                    Email = email,
                    PasswordHash = null,
                    Username = UsernameGenerator.Generate(),
                    GoogleId = googleId,
                    Currency = request.Currency,
                    Name = payload.Name,
                    IsVerified = true,
                    VerifiedAt = now
                };

                db.Users.Add(user);
                isNewAccount = true;

                eventBus.Publish(new UserRegisteredEvent
                {
                    UserId = user.Id,
                    Email = user.Email
                });
            }
        }

        // Cancel pending account deletion if applicable
        if (user.DeletionRequestedAt is not null)
        {
            user.DeletionRequestedAt = null;
            user.UpdatedAt = now;
        }

        // Issue tokens
        var accessToken = jwtService.GenerateAccessToken(user);
        var rawRefreshToken = RandomString.Generate(64, CharSets.Alphanumeric);
        var refreshTokenHash = TokenHasher.Hash(rawRefreshToken);

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

        tokenCookieService.SetTokenCookies(httpContext, accessToken, rawRefreshToken);

        return new GoogleLoginEndpoint.GoogleLoginResponse(
            user.Id, user.Email, user.Role.ToString(), user.Currency, user.Username, isNewAccount);
    }
}
