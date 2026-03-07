using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Features.Friends.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Friends.DeleteFriendship;

// Deletes a friendship. Either party can delete.
public sealed class DeleteFriendshipHandler(
    AppDbContext db,
    IEventBus eventBus,
    IClock clock,
    ILogger<DeleteFriendshipHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(
        Guid friendshipId,
        Guid userId,
        CancellationToken ct)
    {
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(f => f.Id == friendshipId, ct);

        if (friendship is null)
        {
            return Error.NotFound("Friendship not found.");
        }

        // Only the two friends can delete
        if (friendship.UserAId != userId && friendship.UserBId != userId)
        {
            return Error.Forbidden("You are not part of this friendship.");
        }

        var now = clock.GetCurrentInstant();
        friendship.DeletedAt = now;
        friendship.UpdatedAt = now;

        eventBus.Publish(new FriendshipDeletedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = now,
            FriendshipId = friendshipId,
            UserAId = friendship.UserAId,
            UserBId = friendship.UserBId,
            DeletedByUserId = userId
        });

        await db.SaveChangesAsync(ct);

        logger.FriendshipDeleted(friendshipId, userId);

        return true;
    }
}
