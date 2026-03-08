using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.TransferWallet;

// Transfers ownership of a personal wallet to another user.
// The current owner becomes a member with Guest role; the new owner becomes Owner.
// Requires the two users to be confirmed friends.
public sealed class TransferWalletHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(
        Guid walletId,
        Guid callerId,
        Guid toUserId,
        CancellationToken ct)
    {
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.NotFound("Wallet not found.");
        }

        // Only personal wallets can be transferred
        if (wallet.Type != WalletType.Personal)
        {
            return Error.Validation("Only personal wallets can be transferred. Convert to private first.");
        }

        // Caller must be the current Owner
        var callerRole = await WalletAccessChecker.GetRoleAsync(db, walletId, callerId, ct);
        if (callerRole != WalletMemberRole.Owner)
        {
            return Error.Forbidden("Only the wallet owner can transfer it.");
        }

        // Target user must exist
        var toUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == toUserId, ct);
        if (toUser is null)
        {
            return Error.NotFound("Target user not found.");
        }

        // Caller and target must be confirmed friends
        var (lo, hi) = callerId < toUserId ? (callerId, toUserId) : (toUserId, callerId);
        var areFriends = await db.Friendships.AnyAsync(f => f.UserAId == lo && f.UserBId == hi, ct);
        if (!areFriends)
        {
            return Error.Validation("You must be friends with the target user to transfer a wallet.");
        }

        var now = clock.GetCurrentInstant();

        // Demote current owner to Guest
        var callerMember = await db.WalletMembers
            .FirstOrDefaultAsync(m => m.WalletId == walletId && m.UserId == callerId, ct);
        if (callerMember is not null)
        {
            callerMember.Role = WalletMemberRole.Guest;
            callerMember.UpdatedAt = now;
        }

        // Promote target user to Owner — create member row if not already a member
        var toMember = await db.WalletMembers
            .FirstOrDefaultAsync(m => m.WalletId == walletId && m.UserId == toUserId, ct);

        if (toMember is not null)
        {
            toMember.Role = WalletMemberRole.Owner;
            toMember.UpdatedAt = now;
        }
        else
        {
            db.WalletMembers.Add(new WalletMember
            {
                WalletId = walletId,
                UserId = toUserId,
                Role = WalletMemberRole.Owner
            });
        }

        wallet.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        return true;
    }
}
