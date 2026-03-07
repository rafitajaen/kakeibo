using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Friends.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Wallets.Events;

// When a friendship is deleted, removes non-Owner WalletMembers from shared wallets
// where both users were members. Owners always retain their access.
public sealed class FriendshipDeletedHandler(AppDbContext db)
    : IEventHandler<FriendshipDeletedEvent>
{
    public async Task HandleAsync(FriendshipDeletedEvent ev, CancellationToken ct)
    {
        var userA = ev.UserAId;
        var userB = ev.UserBId;

        // Find shared wallets where both users are currently members
        var sharedWalletIds = await db.Wallets
            .Where(w =>
                w.Type == WalletType.Shared &&
                w.DeletedAt == null &&
                w.WalletMembers.Any(m => m.UserId == userA) &&
                w.WalletMembers.Any(m => m.UserId == userB))
            .Select(w => w.Id)
            .ToListAsync(ct);

        if (sharedWalletIds.Count == 0)
        {
            return;
        }

        // Load all members of those wallets for userA and userB
        var membersToEvaluate = await db.WalletMembers
            .Where(m =>
                sharedWalletIds.Contains(m.WalletId) &&
                (m.UserId == userA || m.UserId == userB))
            .ToListAsync(ct);

        var removals = membersToEvaluate
            .Where(m => m.Role != WalletMemberRole.Owner)
            .ToList();

        if (removals.Count > 0)
        {
            // Hard-delete the memberships — consistent with RemoveMemberHandler
            db.WalletMembers.RemoveRange(removals);
            await db.SaveChangesAsync(ct);
        }
    }
}
