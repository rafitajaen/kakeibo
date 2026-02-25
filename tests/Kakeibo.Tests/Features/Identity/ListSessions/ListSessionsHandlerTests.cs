using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.ListSessions;
using NodaTime;
using NSubstitute;
using RefreshTokenEntity = Kakeibo.Api.Domain.Entities.RefreshToken;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.ListSessions;

public sealed class ListSessionsHandlerTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 3, 1, 12, 0);

    private static ListSessionsHandler CreateHandler(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Now);
        return new ListSessionsHandler(db, clock);
    }

    private static UserEntity SeedUser(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new UserEntity
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Currency = "USD",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 1, 1, 0, 0)
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task HandleAsync_ActiveSessions_ReturnsThem()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            TokenHash = "hash1",
            UserId = user.Id,
            ExpiresAt = Now.Plus(Duration.FromDays(7)),
            IpAddress = "127.0.0.1"
        });
        db.SaveChanges();

        var result = await CreateHandler(db).HandleAsync(user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Single(result.Value.Sessions));
    }

    [Fact]
    public async Task HandleAsync_RevokedSession_ExcludesFromResult()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            TokenHash = "revokedhash",
            UserId = user.Id,
            ExpiresAt = Now.Plus(Duration.FromDays(7)),
            RevokedAt = Now.Minus(Duration.FromDays(1))
        });
        db.SaveChanges();

        var result = await CreateHandler(db).HandleAsync(user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Sessions);
    }

    [Fact]
    public async Task HandleAsync_ExpiredSession_ExcludesFromResult()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            TokenHash = "expiredhash",
            UserId = user.Id,
            ExpiresAt = Now.Minus(Duration.FromDays(1))
        });
        db.SaveChanges();

        var result = await CreateHandler(db).HandleAsync(user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Sessions);
    }

    [Fact]
    public async Task HandleAsync_NoSessions_ReturnsEmptyList()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var result = await CreateHandler(db).HandleAsync(user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Empty(result.Value.Sessions));
    }
}
