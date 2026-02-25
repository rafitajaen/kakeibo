using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.ChangePassword;
using NodaTime;
using NSubstitute;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.ChangePassword;

public sealed class ChangePasswordHandlerTests
{
    private const string OriginalPassword = "Original1!";
    private const string NewPassword = "NewPass1!";

    private static ChangePasswordHandler CreateHandler(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 3, 1, 12, 0));
        return new ChangePasswordHandler(db, clock);
    }

    private static UserEntity SeedUser(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new UserEntity
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword(OriginalPassword),
            Currency = "USD",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 1, 1, 0, 0)
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task HandleAsync_CorrectCurrentPassword_ChangesPassword()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new ChangePasswordEndpoint.ChangePasswordRequest(OriginalPassword, NewPassword, NewPassword);
        var result = await CreateHandler(db).HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        // Verify the new password hash is stored
        await db.Entry(user).ReloadAsync(ct);
        Assert.True(PasswordHasher.VerifyPassword(NewPassword, user.PasswordHash));
    }

    [Fact]
    public async Task HandleAsync_WrongCurrentPassword_ReturnsUnauthorized()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new ChangePasswordEndpoint.ChangePasswordRequest("WrongPass1!", NewPassword, NewPassword);
        var result = await CreateHandler(db).HandleAsync(request, user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("unauthorized", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_SamePasswordAsCurrentOne_ReturnsValidationError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new ChangePasswordEndpoint.ChangePasswordRequest(OriginalPassword, OriginalPassword, OriginalPassword);
        var result = await CreateHandler(db).HandleAsync(request, user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("validation", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var request = new ChangePasswordEndpoint.ChangePasswordRequest(OriginalPassword, NewPassword, NewPassword);
        var result = await CreateHandler(db).HandleAsync(request, Guid.NewGuid(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
