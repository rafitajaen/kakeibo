using System.Security.Claims;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.UpdateDisplayPreferences;
using Kakeibo.Api.Persistence;
using Microsoft.AspNetCore.Http;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.UpdateDisplayPreferences;

public sealed class UpdateDisplayPreferencesHandlerTests
{
    private static readonly FakeClock TestClock =
        new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private static UpdateDisplayPreferencesHandler CreateHandler(AppDbContext db) =>
        new(db, TestClock);

    private static UserEntity SeedUser(AppDbContext db)
    {
        var user = new UserEntity
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Username = $"user_{Guid.NewGuid():N}"[..12],
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 1, 1, 0, 0)
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
    public async Task HandleAsync_UpdateWeekStartDay_PersistsChange()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new UpdateDisplayPreferencesEndpoint.UpdateDisplayPreferencesRequest(
            WeekStartDay: 0, // Sunday
            MonthStartDay: null,
            CurrencyDecimalSeparator: null,
            CurrencyGroupSeparator: null,
            CurrencySymbolPosition: null,
            CurrencyDisplay: null);

        var result = await CreateHandler(db).HandleAsync(request, CreateAuthenticatedContext(user.Id), ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(0, result.Value.WeekStartDay));
    }

    [Fact]
    public async Task HandleAsync_UpdateAllFields_PersistsAllChanges()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new UpdateDisplayPreferencesEndpoint.UpdateDisplayPreferencesRequest(
            WeekStartDay: 6,
            MonthStartDay: 15,
            CurrencyDecimalSeparator: ",",
            CurrencyGroupSeparator: ".",
            CurrencySymbolPosition: "after",
            CurrencyDisplay: "code");

        var result = await CreateHandler(db).HandleAsync(request, CreateAuthenticatedContext(user.Id), ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(6, result.Value.WeekStartDay),
            () => Assert.Equal(15, result.Value.MonthStartDay),
            () => Assert.Equal(",", result.Value.CurrencyDecimalSeparator),
            () => Assert.Equal(".", result.Value.CurrencyGroupSeparator),
            () => Assert.Equal("after", result.Value.CurrencySymbolPosition),
            () => Assert.Equal("code", result.Value.CurrencyDisplay));
    }

    [Fact]
    public async Task HandleAsync_NoFieldsProvided_ReturnsCurrentDefaults()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new UpdateDisplayPreferencesEndpoint.UpdateDisplayPreferencesRequest(
            null, null, null, null, null, null);

        var result = await CreateHandler(db).HandleAsync(request, CreateAuthenticatedContext(user.Id), ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            // Defaults should be unchanged
            () => Assert.Equal(1, result.Value.WeekStartDay),
            () => Assert.Equal(1, result.Value.MonthStartDay),
            () => Assert.Equal(".", result.Value.CurrencyDecimalSeparator),
            () => Assert.Equal(",", result.Value.CurrencyGroupSeparator),
            () => Assert.Equal("before", result.Value.CurrencySymbolPosition),
            () => Assert.Equal("symbol", result.Value.CurrencyDisplay));
    }

    [Fact]
    public async Task HandleAsync_NoUserIdClaim_ReturnsUnauthorizedError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var request = new UpdateDisplayPreferencesEndpoint.UpdateDisplayPreferencesRequest(
            WeekStartDay: 1, null, null, null, null, null);

        var result = await CreateHandler(db).HandleAsync(request, new DefaultHttpContext(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var request = new UpdateDisplayPreferencesEndpoint.UpdateDisplayPreferencesRequest(
            WeekStartDay: 1, null, null, null, null, null);

        var result = await CreateHandler(db).HandleAsync(
            request, CreateAuthenticatedContext(Guid.NewGuid()), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
