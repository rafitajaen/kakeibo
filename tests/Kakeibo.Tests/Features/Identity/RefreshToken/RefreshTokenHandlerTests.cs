using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.RefreshToken;
using Kakeibo.Api.Infrastructure.Auth;
using Kakeibo.Api.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = Kakeibo.Api.Domain.Entities.RefreshToken;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.RefreshToken;

public sealed class RefreshTokenHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private static IOptions<JwtOptions> TestJwtOptions => Options.Create(new JwtOptions
    {
        SecretKey = "test-secret-key-for-jwt-signing-must-be-at-least-32-chars!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    });

    private RefreshTokenHandler CreateHandler(AppDbContext db) =>
        new(db, new JwtService(TestJwtOptions, _clock), new TokenCookieService(TestJwtOptions), TestJwtOptions, _clock);

    // Seeds a verified user and a refresh token. Returns the user and the raw (unhashed) token value.
    private static (UserEntity user, string rawToken) SeedUserWithRefreshToken(
        AppDbContext db,
        Instant now,
        bool revoked = false,
        bool expired = false)
    {
        var user = new UserEntity
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Username = $"user_{Guid.NewGuid():N}"[..12],
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = now.Minus(Duration.FromDays(30))
        };
        db.Users.Add(user);
        db.SaveChanges();

        var rawToken = "valid-refresh-token-64-chars-long-placeholder-padding-xxxxxxxxxx";

        var expiresAt = expired
            ? now.Minus(Duration.FromDays(1))  // expired 1 day ago
            : now.Plus(Duration.FromDays(7));   // expires in 7 days

        var refreshToken = new RefreshTokenEntity
        {
            TokenHash = TokenHasher.Hash(rawToken),
            UserId = user.Id,
            ExpiresAt = expiresAt,
            RevokedAt = revoked ? now.Minus(Duration.FromHours(1)) : null
        };
        db.RefreshTokens.Add(refreshToken);
        db.SaveChanges();

        return (user, rawToken);
    }

    // Creates a DefaultHttpContext with the refresh_token cookie pre-set.
    private static HttpContext HttpContextWithCookie(string rawToken)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = $"refresh_token={rawToken}";
        return httpContext;
    }

    [Fact]
    public async Task HandleAsync_ValidToken_RotatesTokenAndReturnsSuccess()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = _clock.GetCurrentInstant();
        var (user, rawToken) = SeedUserWithRefreshToken(db, now);
        var oldTokenHash = TokenHasher.Hash(rawToken);

        var result = await CreateHandler(db).HandleAsync(HttpContextWithCookie(rawToken), ct);

        var allTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync(ct);

        var oldToken = allTokens.FirstOrDefault(rt => rt.TokenHash == oldTokenHash);
        var newToken = allTokens.FirstOrDefault(rt => rt.TokenHash != oldTokenHash);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(2, allTokens.Count),
            () => Assert.True(oldToken!.IsRevoked),
            () => Assert.NotNull(newToken),
            () => Assert.False(newToken!.IsRevoked));
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_ReturnsUnauthorizedError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (_, rawToken) = SeedUserWithRefreshToken(db, _clock.GetCurrentInstant(), expired: true);

        var result = await CreateHandler(db).HandleAsync(HttpContextWithCookie(rawToken), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_RevokedToken_ReturnsUnauthorizedError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (_, rawToken) = SeedUserWithRefreshToken(db, _clock.GetCurrentInstant(), revoked: true);

        var result = await CreateHandler(db).HandleAsync(HttpContextWithCookie(rawToken), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_MissingCookie_ReturnsUnauthorizedError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var result = await CreateHandler(db).HandleAsync(new DefaultHttpContext(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }
}
