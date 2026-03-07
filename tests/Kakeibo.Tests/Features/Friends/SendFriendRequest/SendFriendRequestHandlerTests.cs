using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Friends.SendFriendRequest;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using UserEntity = Kakeibo.Api.Domain.Entities.User;
using FriendshipEntity = Kakeibo.Api.Domain.Entities.Friendship;

namespace Kakeibo.Tests.Features.Friends.SendFriendRequest;

public sealed class SendFriendRequestHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private SendFriendRequestHandler CreateHandler(AppDbContext db) =>
        new(db, _eventBus, _clock, NullLogger<SendFriendRequestHandler>.Instance);

    private static UserEntity SeedUser(AppDbContext db, string email = "alice@example.com", string username = "user_alice01")
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

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesFriendRequest()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var sender = SeedUser(db, "sender@example.com", "user_sender1");
        var receiver = SeedUser(db, "receiver@example.com", "user_recver1");

        var result = await CreateHandler(db).HandleAsync(
            new SendFriendRequestEndpoint.SendFriendRequestRequest(receiver.Id),
            sender.Id,
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(sender.Id, result.Value.SenderUserId),
            () => Assert.Equal(receiver.Id, result.Value.ReceiverUserId));
    }

    [Fact]
    public async Task HandleAsync_SendToSelf_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = SeedUser(db);

        var result = await CreateHandler(db).HandleAsync(
            new SendFriendRequestEndpoint.SendFriendRequestRequest(user.Id),
            user.Id,
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("validation", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_ReceiverNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var sender = SeedUser(db);

        var result = await CreateHandler(db).HandleAsync(
            new SendFriendRequestEndpoint.SendFriendRequestRequest(Guid.NewGuid()),
            sender.Id,
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_AlreadyFriends_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var sender = SeedUser(db, "sender@example.com", "user_sender1");
        var receiver = SeedUser(db, "receiver@example.com", "user_recver1");

        // Create existing friendship
        var (a, b) = sender.Id.CompareTo(receiver.Id) < 0
            ? (sender.Id, receiver.Id)
            : (receiver.Id, sender.Id);
        db.Friendships.Add(new FriendshipEntity { UserAId = a, UserBId = b });
        db.SaveChanges();

        var result = await CreateHandler(db).HandleAsync(
            new SendFriendRequestEndpoint.SendFriendRequestRequest(receiver.Id),
            sender.Id,
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_DuplicatePendingRequest_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var sender = SeedUser(db, "sender@example.com", "user_sender1");
        var receiver = SeedUser(db, "receiver@example.com", "user_recver1");

        // First request
        await CreateHandler(db).HandleAsync(
            new SendFriendRequestEndpoint.SendFriendRequestRequest(receiver.Id),
            sender.Id,
            ct);

        // Duplicate
        var result = await CreateHandler(db).HandleAsync(
            new SendFriendRequestEndpoint.SendFriendRequestRequest(receiver.Id),
            sender.Id,
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }
}
