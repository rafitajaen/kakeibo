using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Features.Identity.LogoutUser;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RefreshTokenEntity = Kakeibo.Api.Domain.Entities.RefreshToken;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.LogoutUser;

public sealed class LogoutUserHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private LogoutUserHandler CreateHandler(AppDbContext db, IEventBus eventBus) =>
        new(db, eventBus, _clock);

    // Seeds a verified user with one active refresh token. Returns user and raw token value.
    private static (UserEntity user, string rawToken) SeedUserWithActiveToken(
        AppDbContext db,
        Instant now)
    {
        var user = new UserEntity
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = now.Minus(Duration.FromDays(30))
        };
        db.Users.Add(user);
        db.SaveChanges();

        var rawToken = "valid-refresh-token-64-chars-long-placeholder-padding-xxxxxxxxxx";
        var refreshToken = new RefreshTokenEntity
        {
            TokenHash = TokenHasher.Hash(rawToken),
            UserId = user.Id,
            ExpiresAt = now.Plus(Duration.FromDays(7))
        };
        db.RefreshTokens.Add(refreshToken);
        db.SaveChanges();

        return (user, rawToken);
    }

    // Creates an authenticated HttpContext with the given user claim and optional cookie.
    private static HttpContext CreateAuthenticatedContext(Guid userId, string? rawToken = null)
    {
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        if (rawToken is not null)
            httpContext.Request.Headers["Cookie"] = $"refresh_token={rawToken}";
        return httpContext;
    }

    [Fact]
    public async Task HandleAsync_ActiveRefreshToken_RevokesTokenAndPublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = _clock.GetCurrentInstant();
        var (user, rawToken) = SeedUserWithActiveToken(db, now);
        var eventBus = Substitute.For<IEventBus>();

        var result = await CreateHandler(db, eventBus).HandleAsync(
            CreateAuthenticatedContext(user.Id, rawToken), ct);

        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.True(storedToken!.IsRevoked),
            () => eventBus.Received(1).Publish(Arg.Is<UserLoggedOutEvent>(e =>
                e.UserId == user.Id && !e.AllSessions)));
    }

    [Fact]
    public async Task HandleAsync_NoCookie_ReturnsSuccessWithoutRevokingToken()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = _clock.GetCurrentInstant();
        var (user, _) = SeedUserWithActiveToken(db, now);
        var eventBus = Substitute.For<IEventBus>();

        // No cookie provided — only the authenticated user claim is set
        var result = await CreateHandler(db, eventBus).HandleAsync(
            CreateAuthenticatedContext(user.Id), ct);

        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.False(storedToken!.IsRevoked));
    }
}
