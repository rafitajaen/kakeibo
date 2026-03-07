using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Friends.CheckFriendshipImpact;

// Calculates the impact of deleting a friendship on shared wallets.
public sealed class CheckFriendshipImpactHandler(AppDbContext db)
{
    public async Task<Result<CheckFriendshipImpactEndpoint.CheckFriendshipImpactResponse>> HandleAsync(
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

        if (friendship.UserAId != userId && friendship.UserBId != userId)
        {
            return Error.Forbidden("You are not part of this friendship.");
        }

        var otherUserId = friendship.UserAId == userId
            ? friendship.UserBId
            : friendship.UserAId;

        // Find shared wallets where both users are members (owner or WalletMember)
        var sharedWallets = await db.Wallets
            .Where(w => w.Type == WalletType.Shared)
            .Where(w =>
                (w.OwnerId == userId || db.WalletMembers.Any(m => m.WalletId == w.Id && m.UserId == userId)) &&
                (w.OwnerId == otherUserId || db.WalletMembers.Any(m => m.WalletId == w.Id && m.UserId == otherUserId)))
            .Select(w => new CheckFriendshipImpactEndpoint.AffectedWalletResponse(
                w.Id,
                w.Name,
                w.OwnerId == userId ? "Owner" : "Member",
                w.OwnerId != userId)) // Non-owners will lose access
            .ToListAsync(ct);

        // Count personal wallets where the other user is a guest (future: WalletMember on personal wallets)
        var guestAccessRevoked = 0;

        return new CheckFriendshipImpactEndpoint.CheckFriendshipImpactResponse(
            sharedWallets,
            guestAccessRevoked);
    }
}
