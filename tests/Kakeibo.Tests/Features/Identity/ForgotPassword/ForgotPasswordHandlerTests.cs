using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.ForgotPassword;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Identity.ForgotPassword;

public sealed class ForgotPasswordHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private ForgotPasswordHandler CreateHandler(AppDbContext db) =>
        new(db, Substitute.For<IEmailService>(), _clock);

    // Seeds a verified user.
    private static User SeedVerifiedUser(AppDbContext db, Instant now)
    {
        var user = new User
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = now.Minus(Duration.FromDays(7))
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task HandleAsync_ExistingEmail_CreatesResetTokenAndReturnsSuccess()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedVerifiedUser(db, _clock.GetCurrentInstant());

        var result = await CreateHandler(db).HandleAsync(
            new ForgotPasswordEndpoint.ForgotPasswordRequest("alice@example.com"), ct);

        var resetToken = await db.PasswordResetTokens
            .FirstOrDefaultAsync(prt => prt.UserId == user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.NotNull(resetToken),
            () => Assert.True(resetToken!.ExpiresAt > _clock.GetCurrentInstant()),
            () => Assert.Null(resetToken!.UsedAt));
    }

    [Fact]
    public async Task HandleAsync_NonExistentEmail_ReturnsSuccessWithoutCreatingToken()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var result = await CreateHandler(db).HandleAsync(
            new ForgotPasswordEndpoint.ForgotPasswordRequest("nobody@example.com"), ct);

        var tokenCount = await db.PasswordResetTokens.CountAsync(ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(0, tokenCount));
    }
}
