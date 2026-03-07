using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.VerifyEmail;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Identity.VerifyEmail;

public sealed class VerifyEmailHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private VerifyEmailHandler CreateHandler(AppDbContext db) => new(db, _clock);

    // Seeds an unverified user with a valid (or optionally expired) email verification token.
    private static (User user, string rawToken) SeedUnverifiedUserWithToken(
        AppDbContext db,
        Instant now,
        bool expired = false)
    {
        var rawToken = "verify-token-32-chars-long-xxxxx";
        var user = new User
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Username = $"user_{Guid.NewGuid():N}"[..12],
            Currency = "EUR",
            IsVerified = false,
            EmailVerificationToken = TokenHasher.Hash(rawToken),
            EmailVerificationTokenExpiresAt = expired
                ? now.Minus(Duration.FromHours(1))   // expired 1 hour ago
                : now.Plus(Duration.FromHours(24))    // valid for 24 more hours
        };
        db.Users.Add(user);
        db.SaveChanges();
        return (user, rawToken);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_VerifiesEmailAndClearsToken()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = _clock.GetCurrentInstant();
        var (user, rawToken) = SeedUnverifiedUserWithToken(db, now);

        var result = await CreateHandler(db).HandleAsync(
            new VerifyEmailEndpoint.VerifyEmailRequest(rawToken), ct);

        var updatedUser = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.True(updatedUser!.IsVerified),
            () => Assert.NotNull(updatedUser!.VerifiedAt),
            () => Assert.Null(updatedUser!.EmailVerificationToken),
            () => Assert.Null(updatedUser!.EmailVerificationTokenExpiresAt));
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (_, rawToken) = SeedUnverifiedUserWithToken(db, _clock.GetCurrentInstant(), expired: true);

        var result = await CreateHandler(db).HandleAsync(
            new VerifyEmailEndpoint.VerifyEmailRequest(rawToken), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_AlreadyVerifiedUser_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = _clock.GetCurrentInstant();
        var (_, rawToken) = SeedUnverifiedUserWithToken(db, now);

        // First call verifies the user and clears the token
        await CreateHandler(db).HandleAsync(
            new VerifyEmailEndpoint.VerifyEmailRequest(rawToken), ct);

        // Second call with the same token must fail — user is verified, token is cleared
        var result = await CreateHandler(db).HandleAsync(
            new VerifyEmailEndpoint.VerifyEmailRequest(rawToken), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
