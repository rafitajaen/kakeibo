using System.Security.Claims;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.UpdateUsername;
using Kakeibo.Api.Persistence;
using Microsoft.AspNetCore.Http;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.UpdateUsername;

public sealed class UpdateUsernameHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private UpdateUsernameHandler CreateHandler(AppDbContext db) => new(db, _clock);

    private static UserEntity SeedUser(AppDbContext db, string username = "user_abc1234", string email = "alice@example.com")
    {
        var user = new UserEntity
        {
            Email = email,
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Username = username,
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 2, 1, 10, 0)
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    private static HttpContext CreateAuthenticatedContext(Guid userId)
    {
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        return httpContext;
    }

    [Fact]
    public async Task HandleAsync_ValidUsername_UpdatesAndReturnsNewUsername()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var result = await CreateHandler(db).HandleAsync(
            new UpdateUsernameEndpoint.UpdateUsernameRequest("new_username"),
            CreateAuthenticatedContext(user.Id),
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("new_username", result.Value.Username));
    }

    [Fact]
    public async Task HandleAsync_DuplicateUsername_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        SeedUser(db, username: "taken_name", email: "other@example.com");
        var user = SeedUser(db, username: "user_original", email: "alice@example.com");

        var result = await CreateHandler(db).HandleAsync(
            new UpdateUsernameEndpoint.UpdateUsernameRequest("taken_name"),
            CreateAuthenticatedContext(user.Id),
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_UnauthenticatedUser_ReturnsUnauthorized()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var result = await CreateHandler(db).HandleAsync(
            new UpdateUsernameEndpoint.UpdateUsernameRequest("new_name"),
            new DefaultHttpContext(),
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_NormalizesToLowercase()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var result = await CreateHandler(db).HandleAsync(
            new UpdateUsernameEndpoint.UpdateUsernameRequest("My_Name_123"),
            CreateAuthenticatedContext(user.Id),
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("my_name_123", result.Value.Username));
    }
}
