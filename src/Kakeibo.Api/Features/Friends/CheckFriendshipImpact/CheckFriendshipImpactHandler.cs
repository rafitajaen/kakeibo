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

        // Find shared wallets where both users are WalletMembers
        var sharedWallets = await db.Wallets
            .Where(w => w.Type == WalletType.Shared)
            .Where(w =>
                db.WalletMembers.Any(m => m.WalletId == w.Id && m.UserId == userId) &&
                db.WalletMembers.Any(m => m.WalletId == w.Id && m.UserId == otherUserId))
            .Select(w => new
            {
                w.Id,
                w.Name,
                CallerRole = db.WalletMembers
                    .Where(m => m.WalletId == w.Id && m.UserId == userId)
                    .Select(m => m.Role)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var affectedWallets = sharedWallets
            .Select(w => new CheckFriendshipImpactEndpoint.AffectedWalletResponse(
                w.Id,
                w.Name,
                w.CallerRole.ToString(),
                w.CallerRole != WalletMemberRole.Owner)) // Non-owners will lose access
            .ToList();

        // Count personal wallets where the other user is a guest (future: WalletMember on personal wallets)
        var guestAccessRevoked = 0;

        return new CheckFriendshipImpactEndpoint.CheckFriendshipImpactResponse(
            affectedWallets,
            guestAccessRevoked);
    }
}
