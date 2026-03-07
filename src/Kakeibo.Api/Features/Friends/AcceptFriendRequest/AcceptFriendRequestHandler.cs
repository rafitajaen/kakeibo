using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Friends.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Friends.AcceptFriendRequest;

// Accepts a pending friend request and creates a Friendship record.
public sealed class AcceptFriendRequestHandler(
    AppDbContext db,
    IEventBus eventBus,
    IClock clock,
    ILogger<AcceptFriendRequestHandler> logger)
{
    public async Task<Result<AcceptFriendRequestEndpoint.AcceptFriendRequestResponse>> HandleAsync(
        Guid requestId,
        Guid userId,
        CancellationToken ct)
    {
        var now = clock.GetCurrentInstant();

        var request = await db.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null)
        {
            return Error.NotFound("Friend request not found.");
        }

        // Only the receiver can accept
        if (request.ReceiverUserId != userId)
        {
            return Error.Forbidden("Only the receiver can accept this request.");
        }

        if (request.AcceptedAt is not null)
        {
            return Error.Conflict("This request has already been accepted.");
        }

        if (request.RejectedAt is not null)
        {
            return Error.Conflict("This request has already been rejected.");
        }

        request.AcceptedAt = now;
        request.UpdatedAt = now;

        // Create the friendship with normalized IDs
        var (userAId, userBId) = NormalizeIds(request.SenderUserId, request.ReceiverUserId);
        var friendship = new Friendship
        {
            UserAId = userAId,
            UserBId = userBId
        };

        db.Friendships.Add(friendship);

        eventBus.Publish(new FriendRequestAcceptedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = now,
            RequestId = requestId,
            SenderUserId = request.SenderUserId,
            ReceiverUserId = request.ReceiverUserId,
            FriendshipId = friendship.Id
        });

        await db.SaveChangesAsync(ct);

        logger.FriendRequestAccepted(requestId, friendship.Id);

        return new AcceptFriendRequestEndpoint.AcceptFriendRequestResponse(friendship.Id);
    }

    private static (Guid, Guid) NormalizeIds(Guid a, Guid b)
        => a.CompareTo(b) < 0 ? (a, b) : (b, a);
}
