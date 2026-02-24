using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Wallets.RemoveMember;

// Removes a member from a shared wallet. Owner can remove any non-owner; non-owner can only remove themselves.
public sealed class RemoveMemberHandler(AppDbContext db, IEventBus eventBus)
{
    public async Task<Result<bool>> HandleAsync(
        Guid walletId,
        Guid targetUserId,
        Guid requesterId,
        CancellationToken ct)
    {
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);
        if (wallet is null)
            return Error.NotFound("Wallet not found.");

        // Owner cannot be removed via this endpoint (ownership transfer not supported in MVP)
        if (targetUserId == wallet.OwnerId)
            return Error.Forbidden("The wallet owner cannot be removed.");

        // Non-owner can only remove themselves; owner can remove anyone (except themselves)
        var requesterIsOwner = requesterId == wallet.OwnerId;
        var requesterRemovesSelf = requesterId == targetUserId;

        if (!requesterIsOwner && !requesterRemovesSelf)
            return Error.Forbidden("You can only remove yourself from this wallet.");

        var member = await db.WalletMembers
            .FirstOrDefaultAsync(m => m.WalletId == walletId && m.UserId == targetUserId, ct);

        if (member is null)
            return Error.NotFound("Member not found in this wallet.");

        // Enforce minimum 2 members — a shared wallet cannot drop to a single participant
        var memberCount = await db.WalletMembers.CountAsync(m => m.WalletId == walletId, ct);
        if (memberCount <= 1)
            return Error.Validation("A shared wallet must have at least 2 members. Archive the wallet instead of removing the last member.");

        // Hard-delete WalletMember — audit trail is in ClickHouse (plan decision)
        db.WalletMembers.Remove(member);

        eventBus.Publish(new MemberLeftEvent
        {
            WalletId = walletId,
            UserId = targetUserId
        });

        await db.SaveChangesAsync(ct);

        return true;
    }
}
