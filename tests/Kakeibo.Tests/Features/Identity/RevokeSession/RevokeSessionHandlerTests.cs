using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.RevokeSession;
using NodaTime;
using NSubstitute;
using RefreshTokenEntity = Kakeibo.Api.Domain.Entities.RefreshToken;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.RevokeSession;

public sealed class RevokeSessionHandlerTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 3, 1, 12, 0);

    private static RevokeSessionHandler CreateHandler(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Now);
        return new RevokeSessionHandler(db, clock);
    }

    private static UserEntity SeedUser(Kakeibo.Api.Persistence.AppDbContext db, string email = "alice@example.com")
    {
        var user = new UserEntity
        {
            Email = email,
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
    public async Task HandleAsync_OwnActiveSession_RevokesIt()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var token = new RefreshTokenEntity
        {
            TokenHash = "activehash",
            UserId = user.Id,
            ExpiresAt = Now.Plus(Duration.FromDays(7))
        };
        db.RefreshTokens.Add(token);
        db.SaveChanges();

        var result = await CreateHandler(db).HandleAsync(token.Id, user.Id, ct);

        Assert.True(result.IsSuccess);
        await db.Entry(token).ReloadAsync(ct);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task HandleAsync_SessionNotFound_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var result = await CreateHandler(db).HandleAsync(Guid.NewGuid(), user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_OtherUsersSession_ReturnsForbiddenError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var alice = SeedUser(db, "alice@example.com");
        var bob = SeedUser(db, "bob@example.com");

        var token = new RefreshTokenEntity
        {
            TokenHash = "bobhash",
            UserId = bob.Id,
            ExpiresAt = Now.Plus(Duration.FromDays(7))
        };
        db.RefreshTokens.Add(token);
        db.SaveChanges();

        // Alice tries to revoke Bob's session
        var result = await CreateHandler(db).HandleAsync(token.Id, alice.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_AlreadyRevokedSession_ReturnsSuccessIdempotently()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var token = new RefreshTokenEntity
        {
            TokenHash = "alreadyrevoked",
            UserId = user.Id,
            ExpiresAt = Now.Plus(Duration.FromDays(7)),
            RevokedAt = Now.Minus(Duration.FromDays(1))
        };
        db.RefreshTokens.Add(token);
        db.SaveChanges();

        var result = await CreateHandler(db).HandleAsync(token.Id, user.Id, ct);

        // Idempotent — already revoked is still a success
        Assert.True(result.IsSuccess);
    }
}
