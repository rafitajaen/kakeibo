using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Friends.DeleteFriendship;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Friends.DeleteFriendship;

public sealed class DeleteFriendshipHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private DeleteFriendshipHandler CreateHandler(AppDbContext db) =>
        new(db, _eventBus, _clock, NullLogger<DeleteFriendshipHandler>.Instance);

    private static (UserEntity, UserEntity, Friendship) SeedFriendship(AppDbContext db)
    {
        var userA = new UserEntity
        {
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Username = "user_alice01",
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 2, 1, 10, 0)
        };
        var userB = new UserEntity
        {
            Email = "bob@example.com",
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Username = "user_bobb001",
            Currency = "EUR",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 2, 1, 10, 0)
        };
        db.Users.AddRange(userA, userB);
        db.SaveChanges();

        var (a, b) = userA.Id.CompareTo(userB.Id) < 0
            ? (userA.Id, userB.Id)
            : (userB.Id, userA.Id);
        var friendship = new Friendship { UserAId = a, UserBId = b };
        db.Friendships.Add(friendship);
        db.SaveChanges();

        return (userA, userB, friendship);
    }

    [Fact]
    public async Task HandleAsync_ValidDeletion_SoftDeletesFriendship()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (userA, _, friendship) = SeedFriendship(db);

        var result = await CreateHandler(db).HandleAsync(friendship.Id, userA.Id, ct);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_NonParticipant_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (_, _, friendship) = SeedFriendship(db);

        var result = await CreateHandler(db).HandleAsync(friendship.Id, Guid.NewGuid(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var result = await CreateHandler(db).HandleAsync(Guid.NewGuid(), Guid.NewGuid(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
