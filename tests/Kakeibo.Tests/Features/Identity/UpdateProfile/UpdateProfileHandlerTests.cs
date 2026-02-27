using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Identity.UpdateProfile;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.UpdateProfile;

public sealed class UpdateProfileHandlerTests
{
    private static UpdateProfileHandler CreateHandler(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 3, 1, 12, 0));
        return new UpdateProfileHandler(db, clock);
    }

    private static UserEntity SeedUser(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new UserEntity
        {
            Email = "alice@example.com",
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
    public async Task HandleAsync_UpdateName_ReturnsUpdatedProfile()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new UpdateProfileEndpoint.UpdateProfileRequest("Alice Smith", null);
        var result = await CreateHandler(db).HandleAsync(request, user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("Alice Smith", result.Value.Name));
    }

    [Fact]
    public async Task HandleAsync_UpdateCurrency_ReturnsUppercasedCurrency()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new UpdateProfileEndpoint.UpdateProfileRequest(null, "eur");
        var result = await CreateHandler(db).HandleAsync(request, user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("EUR", result.Value.Currency));
    }

    [Fact]
    public async Task HandleAsync_EmptyName_SetsNameToNull()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new UpdateProfileEndpoint.UpdateProfileRequest("   ", null);
        var result = await CreateHandler(db).HandleAsync(request, user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Null(result.Value.Name));
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var request = new UpdateProfileEndpoint.UpdateProfileRequest("Test", null);
        var result = await CreateHandler(db).HandleAsync(request, Guid.NewGuid(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_NoFieldsProvided_ReturnsCurrentProfile()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var request = new UpdateProfileEndpoint.UpdateProfileRequest(null, null);
        var result = await CreateHandler(db).HandleAsync(request, user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("USD", result.Value.Currency));
    }
}
