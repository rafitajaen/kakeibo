using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Features.Friends.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Friends.SendFriendRequest;

// Sends a friend request from the authenticated user to another user.
public sealed class SendFriendRequestHandler(
    AppDbContext db,
    IEventBus eventBus,
    IClock clock,
    ILogger<SendFriendRequestHandler> logger)
{
    public async Task<Result<SendFriendRequestEndpoint.SendFriendRequestResponse>> HandleAsync(
        SendFriendRequestEndpoint.SendFriendRequestRequest request,
        Guid senderUserId,
        CancellationToken ct)
    {
        // Cannot send a friend request to yourself
        if (request.ReceiverUserId == senderUserId)
        {
            return Error.Validation("Cannot send a friend request to yourself.");
        }

        // Verify receiver exists
        var receiverExists = await db.Users.AnyAsync(u => u.Id == request.ReceiverUserId, ct);
        if (!receiverExists)
        {
            return Error.NotFound("User not found.");
        }

        // Check if already friends (normalize IDs for lookup)
        var (userAId, userBId) = NormalizeIds(senderUserId, request.ReceiverUserId);
        var alreadyFriends = await db.Friendships
            .AnyAsync(f => f.UserAId == userAId && f.UserBId == userBId, ct);

        if (alreadyFriends)
        {
            return Error.Conflict("You are already friends with this user.");
        }

        // Check for existing pending request in either direction
        var existingRequest = await db.FriendRequests
            .FirstOrDefaultAsync(r =>
                ((r.SenderUserId == senderUserId && r.ReceiverUserId == request.ReceiverUserId) ||
                 (r.SenderUserId == request.ReceiverUserId && r.ReceiverUserId == senderUserId)) &&
                r.AcceptedAt == null && r.RejectedAt == null, ct);

        if (existingRequest is not null)
        {
            return Error.Conflict("A pending friend request already exists between you and this user.");
        }

        var friendRequest = new Domain.Entities.FriendRequest
        {
            SenderUserId = senderUserId,
            ReceiverUserId = request.ReceiverUserId
        };

        db.FriendRequests.Add(friendRequest);

        eventBus.Publish(new FriendRequestSentEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = clock.GetCurrentInstant(),
            RequestId = friendRequest.Id,
            SenderUserId = senderUserId,
            ReceiverUserId = request.ReceiverUserId
        });

        await db.SaveChangesAsync(ct);

        logger.FriendRequestSent(friendRequest.Id, senderUserId, request.ReceiverUserId);

        return new SendFriendRequestEndpoint.SendFriendRequestResponse(
            friendRequest.Id,
            senderUserId,
            request.ReceiverUserId);
    }

    // Normalizes two GUIDs so the smaller one is always first.
    private static (Guid, Guid) NormalizeIds(Guid a, Guid b)
        => a.CompareTo(b) < 0 ? (a, b) : (b, a);
}
