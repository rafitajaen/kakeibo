using System.Security.Claims;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.GetCurrentUser;
using Kakeibo.Api.Persistence;
using Microsoft.AspNetCore.Http;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.GetCurrentUser;

public sealed class GetCurrentUserHandlerTests
{
    private static GetCurrentUserHandler CreateHandler(AppDbContext db) => new(db);

    // Seeds a verified user and returns it.
    private static UserEntity SeedVerifiedUser(AppDbContext db)
    {
        var user = new UserEntity
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 2, 1, 10, 0)
        };
        db.Users.Add(user);
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
    public async Task HandleAsync_AuthenticatedUser_ReturnsUserProfile()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedVerifiedUser(db);

        var result = await CreateHandler(db).HandleAsync(
            CreateAuthenticatedContext(user.Id), ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(user.Id, result.Value.Id),
            () => Assert.Equal(user.Email, result.Value.Email),
            () => Assert.Equal(user.Currency, result.Value.Currency),
            () => Assert.True(result.Value.IsVerified));
    }

    [Fact]
    public async Task HandleAsync_NoUserIdClaim_ReturnsUnauthorizedError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        // Default HttpContext has no user claims
        var result = await CreateHandler(db).HandleAsync(new DefaultHttpContext(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_ValidClaimButUserNotInDb_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        // Use a valid GUID that has no matching user in the database
        var result = await CreateHandler(db).HandleAsync(
            CreateAuthenticatedContext(Guid.NewGuid()), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
