using System.Security.Claims;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Features.Identity.LogoutAllSessions;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = Kakeibo.Api.Domain.Entities.RefreshToken;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.LogoutAllSessions;

public sealed class LogoutAllSessionsHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private LogoutAllSessionsHandler CreateHandler(AppDbContext db, IEventBus eventBus) =>
        new(db, eventBus, _clock);

    // Seeds a verified user and N active refresh tokens. Returns the user.
    private static UserEntity SeedUserWithTokens(AppDbContext db, Instant now, int tokenCount)
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

        for (var i = 0; i < tokenCount; i++)
        {
            var rawToken = $"token-{i}-32-chars-long-placeholder-xxxxx";
            db.RefreshTokens.Add(new RefreshTokenEntity
            {
                TokenHash = TokenHasher.Hash(rawToken),
                UserId = user.Id,
                ExpiresAt = now.Plus(Duration.FromDays(7))
            });
        }

        db.SaveChanges();
        return user;
    }

    // Creates an authenticated HttpContext with the given userId claim.
    private static HttpContext CreateAuthenticatedContext(Guid userId)
    {
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        return httpContext;
    }

    [Fact]
    public async Task HandleAsync_MultipleActiveSessions_RevokesAllAndPublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = _clock.GetCurrentInstant();
        var user = SeedUserWithTokens(db, now, tokenCount: 3);
        var eventBus = Substitute.For<IEventBus>();

        var result = await CreateHandler(db, eventBus).HandleAsync(
            CreateAuthenticatedContext(user.Id), ct);

        var activeTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .CountAsync(ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(3, result.Value.RevokedCount),
            () => Assert.Equal(0, activeTokens),
            () => eventBus.Received(1).Publish(Arg.Is<UserLoggedOutEvent>(e =>
                e.UserId == user.Id && e.AllSessions)));
    }

    [Fact]
    public async Task HandleAsync_NoActiveSessions_ReturnsZeroRevokedCount()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = _clock.GetCurrentInstant();
        // Seed user with no refresh tokens
        var user = SeedUserWithTokens(db, now, tokenCount: 0);
        var eventBus = Substitute.For<IEventBus>();

        var result = await CreateHandler(db, eventBus).HandleAsync(
            CreateAuthenticatedContext(user.Id), ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(0, result.Value.RevokedCount));
    }

    [Fact]
    public async Task HandleAsync_Unauthenticated_ReturnsUnauthorizedError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        // Default HttpContext has no user claims
        var result = await CreateHandler(db, eventBus).HandleAsync(new DefaultHttpContext(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }
}
