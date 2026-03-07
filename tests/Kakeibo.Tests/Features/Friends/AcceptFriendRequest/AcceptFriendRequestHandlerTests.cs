using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Friends.AcceptFriendRequest;
using Kakeibo.Api.Features.Friends.SendFriendRequest;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Friends.AcceptFriendRequest;

public sealed class AcceptFriendRequestHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private AcceptFriendRequestHandler CreateAcceptHandler(AppDbContext db) =>
        new(db, _eventBus, _clock, NullLogger<AcceptFriendRequestHandler>.Instance);

    private SendFriendRequestHandler CreateSendHandler(AppDbContext db) =>
        new(db, _eventBus, _clock, NullLogger<SendFriendRequestHandler>.Instance);

    private static UserEntity SeedUser(AppDbContext db, string email, string username)
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
    public async Task HandleAsync_ValidAccept_CreatesFriendship()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var sender = SeedUser(db, "sender@example.com", "user_sender1");
        var receiver = SeedUser(db, "receiver@example.com", "user_recver1");

        // Send request
        var sendResult = await CreateSendHandler(db).HandleAsync(
            new SendFriendRequestEndpoint.SendFriendRequestRequest(receiver.Id),
            sender.Id,
            ct);

        // Accept
        var result = await CreateAcceptHandler(db).HandleAsync(
            sendResult.Value.Id,
            receiver.Id,
            ct);

        Assert.True(result.IsSuccess);

        // Verify friendship exists
        var friendship = await db.Friendships.FirstOrDefaultAsync(ct);
        Assert.NotNull(friendship);
    }

    [Fact]
    public async Task HandleAsync_SenderTriesToAccept_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var sender = SeedUser(db, "sender@example.com", "user_sender1");
        var receiver = SeedUser(db, "receiver@example.com", "user_recver1");

        var sendResult = await CreateSendHandler(db).HandleAsync(
            new SendFriendRequestEndpoint.SendFriendRequestRequest(receiver.Id),
            sender.Id,
            ct);

        var result = await CreateAcceptHandler(db).HandleAsync(
            sendResult.Value.Id,
            sender.Id, // sender tries to accept own request
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_AlreadyAccepted_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var sender = SeedUser(db, "sender@example.com", "user_sender1");
        var receiver = SeedUser(db, "receiver@example.com", "user_recver1");

        var sendResult = await CreateSendHandler(db).HandleAsync(
            new SendFriendRequestEndpoint.SendFriendRequestRequest(receiver.Id),
            sender.Id,
            ct);

        // Accept first time
        await CreateAcceptHandler(db).HandleAsync(sendResult.Value.Id, receiver.Id, ct);

        // Accept second time
        var result = await CreateAcceptHandler(db).HandleAsync(sendResult.Value.Id, receiver.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_RequestNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var result = await CreateAcceptHandler(db).HandleAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
