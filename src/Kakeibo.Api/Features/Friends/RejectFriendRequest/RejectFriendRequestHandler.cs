using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Features.Friends.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Friends.RejectFriendRequest;

// Rejects a pending friend request. Only the receiver can reject.
public sealed class RejectFriendRequestHandler(
    AppDbContext db,
    IEventBus eventBus,
    IClock clock,
    ILogger<RejectFriendRequestHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(
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

        if (request.ReceiverUserId != userId)
        {
            return Error.Forbidden("Only the receiver can reject this request.");
        }

        if (request.AcceptedAt is not null)
        {
            return Error.Conflict("This request has already been accepted.");
        }

        if (request.RejectedAt is not null)
        {
            return Error.Conflict("This request has already been rejected.");
        }

        request.RejectedAt = now;
        request.UpdatedAt = now;

        eventBus.Publish(new FriendRequestRejectedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = now,
            RequestId = requestId,
            SenderUserId = request.SenderUserId,
            ReceiverUserId = request.ReceiverUserId
        });

        await db.SaveChangesAsync(ct);

        logger.FriendRequestRejected(requestId);

        return true;
    }
}
