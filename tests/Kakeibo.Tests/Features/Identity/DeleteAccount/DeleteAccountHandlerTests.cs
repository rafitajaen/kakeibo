using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.DeleteAccount;
using Microsoft.AspNetCore.Http;
using RefreshTokenEntity = Kakeibo.Api.Domain.Entities.RefreshToken;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.DeleteAccount;

public sealed class DeleteAccountHandlerTests
{
    private const string Password = "Test1234!";

    private static DeleteAccountHandler CreateHandler(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 3, 1, 12, 0));
        return new DeleteAccountHandler(db, clock);
    }

    private static UserEntity SeedUser(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new UserEntity
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword(Password),
            Username = $"user_{Guid.NewGuid():N}"[..12],
            Currency = "USD",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 1, 1, 0, 0)
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task HandleAsync_CorrectPassword_SetsDeletionRequestedAt()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new DeleteAccountEndpoint.DeleteAccountRequest(Password);
        var result = await CreateHandler(db).HandleAsync(request, user.Id, new DefaultHttpContext(), ct);

        Assert.True(result.IsSuccess);
        await db.Entry(user).ReloadAsync(ct);
        Assert.NotNull(user.DeletionRequestedAt);
    }

    [Fact]
    public async Task HandleAsync_CorrectPassword_RevokesAllActiveSessions()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        // Seed an active session
        var token = new RefreshTokenEntity
        {
            TokenHash = "somehash",
            UserId = user.Id,
            ExpiresAt = Instant.FromUtc(2026, 12, 1, 0, 0)
        };
        db.RefreshTokens.Add(token);
        db.SaveChanges();

        var request = new DeleteAccountEndpoint.DeleteAccountRequest(Password);
        await CreateHandler(db).HandleAsync(request, user.Id, new DefaultHttpContext(), ct);

        await db.Entry(token).ReloadAsync(ct);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ReturnsUnauthorized()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new DeleteAccountEndpoint.DeleteAccountRequest("WrongPass1!");
        var result = await CreateHandler(db).HandleAsync(request, user.Id, new DefaultHttpContext(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var request = new DeleteAccountEndpoint.DeleteAccountRequest(Password);
        var result = await CreateHandler(db).HandleAsync(request, Guid.NewGuid(), new DefaultHttpContext(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
