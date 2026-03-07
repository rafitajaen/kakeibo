using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Friends.CancelFriendRequest;

// Cancels a pending friend request. Only the sender can cancel.
public sealed class CancelFriendRequestHandler(
    AppDbContext db,
    IClock clock,
    ILogger<CancelFriendRequestHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(
        Guid requestId,
        Guid userId,
        CancellationToken ct)
    {
        var request = await db.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null)
        {
            return Error.NotFound("Friend request not found.");
        }

        if (request.SenderUserId != userId)
        {
            return Error.Forbidden("Only the sender can cancel this request.");
        }

        if (request.AcceptedAt is not null)
        {
            return Error.Conflict("This request has already been accepted.");
        }

        if (request.RejectedAt is not null)
        {
            return Error.Conflict("This request has already been rejected.");
        }

        // Soft delete the request
        var now = clock.GetCurrentInstant();
        request.DeletedAt = now;
        request.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        logger.FriendRequestCancelled(requestId);

        return true;
    }
}
