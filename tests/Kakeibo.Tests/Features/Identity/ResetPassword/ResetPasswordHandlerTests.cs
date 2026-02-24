using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.ResetPassword;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Identity.ResetPassword;

public sealed class ResetPasswordHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private ResetPasswordHandler CreateHandler(AppDbContext db) => new(db, _clock);

    // Seeds a verified user and a password reset token. Returns the user and the raw (unhashed) token value.
    private static (User user, string rawToken) SeedUserWithResetToken(
        AppDbContext db,
        Instant now,
        bool expired = false,
        bool used = false)
    {
        var user = new User
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword("OldPassword1!"),
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = now.Minus(Duration.FromDays(7))
        };
        db.Users.Add(user);
        db.SaveChanges();

        var rawToken = "reset-token-32-chars-long-xxxxxY";

        var expiresAt = expired
            ? now.Minus(Duration.FromHours(1))  // expired 1 hour ago
            : now.Plus(Duration.FromHours(1));   // valid for 1 more hour

        var resetToken = new PasswordResetToken
        {
            TokenHash = TokenHasher.Hash(rawToken),
            UserId = user.Id,
            ExpiresAt = expiresAt,
            UsedAt = used ? now.Minus(Duration.FromMinutes(10)) : null
        };
        db.PasswordResetTokens.Add(resetToken);
        db.SaveChanges();

        return (user, rawToken);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_ChangesPasswordAndSetsUsedAt()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, rawToken) = SeedUserWithResetToken(db, _clock.GetCurrentInstant());

        var result = await CreateHandler(db).HandleAsync(
            new ResetPasswordEndpoint.ResetPasswordRequest(rawToken, "NewPassword1!", "NewPassword1!"), ct);

        var updatedUser = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, ct);
        var usedToken = await db.PasswordResetTokens
            .FirstOrDefaultAsync(prt => prt.UserId == user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.True(PasswordHasher.VerifyPassword("NewPassword1!", updatedUser!.PasswordHash)),
            () => Assert.False(PasswordHasher.VerifyPassword("OldPassword1!", updatedUser!.PasswordHash)),
            () => Assert.NotNull(usedToken!.UsedAt));
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (_, rawToken) = SeedUserWithResetToken(db, _clock.GetCurrentInstant(), expired: true);

        var result = await CreateHandler(db).HandleAsync(
            new ResetPasswordEndpoint.ResetPasswordRequest(rawToken, "NewPassword1!", "NewPassword1!"), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_AlreadyUsedToken_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (_, rawToken) = SeedUserWithResetToken(db, _clock.GetCurrentInstant(), used: true);

        var result = await CreateHandler(db).HandleAsync(
            new ResetPasswordEndpoint.ResetPasswordRequest(rawToken, "NewPassword1!", "NewPassword1!"), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
