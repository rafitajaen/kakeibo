using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.LoginUser;
using Kakeibo.Api.Infrastructure.Auth;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kakeibo.Tests.Features.Identity.LoginUser;

public sealed class LoginUserHandlerTests
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

    private LoginUserHandler CreateHandler(Kakeibo.Api.Persistence.AppDbContext db) =>
        new(db, new JwtService(TestJwtOptions, _clock), new TokenCookieService(TestJwtOptions), Substitute.For<IEventBus>(), TestJwtOptions, _clock);

    // Seeds a verified user and saves it synchronously.
    private static User SeedVerifiedUser(
        Kakeibo.Api.Persistence.AppDbContext db,
        string email = "alice@example.com",
        string password = "Test1234!")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHasher.HashPassword(password),
            Username = $"user_{Guid.NewGuid():N}"[..12],
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 2, 1, 10, 0)
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task HandleAsync_ValidCredentialsVerifiedUser_ReturnsResponse()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedVerifiedUser(db);

        var result = await CreateHandler(db).HandleAsync(
            new LoginUserEndpoint.LoginUserRequest(user.Email, "Test1234!"),
            new DefaultHttpContext(),
            ct);

        var refreshTokenInDb = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(user.Id, result.Value.Id),
            () => Assert.Equal(user.Email, result.Value.Email),
            () => Assert.NotNull(refreshTokenInDb),
            () => Assert.False(refreshTokenInDb!.IsRevoked));
    }

    [Fact]
    public async Task HandleAsync_UnverifiedEmail_ReturnsValidationError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = new User
        {
            Email = "unverified@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Username = $"user_{Guid.NewGuid():N}"[..12],
            Currency = "EUR",
            IsVerified = false
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var result = await CreateHandler(db).HandleAsync(
            new LoginUserEndpoint.LoginUserRequest("unverified@example.com", "Test1234!"),
            new DefaultHttpContext(),
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("validation", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ReturnsUnauthorizedError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        SeedVerifiedUser(db);

        var result = await CreateHandler(db).HandleAsync(
            new LoginUserEndpoint.LoginUserRequest("alice@example.com", "WrongPassword1!"),
            new DefaultHttpContext(),
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_NonExistentEmail_ReturnsUnauthorizedError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var result = await CreateHandler(db).HandleAsync(
            new LoginUserEndpoint.LoginUserRequest("nobody@example.com", "Test1234!"),
            new DefaultHttpContext(),
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }
}
