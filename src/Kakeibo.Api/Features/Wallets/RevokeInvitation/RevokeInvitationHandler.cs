using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.RevokeInvitation;

// Revokes a pending invitation. Only the wallet Owner or the original inviter can revoke.
public sealed class RevokeInvitationHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(
        Guid walletId,
        string code,
        Guid requesterId,
        CancellationToken ct)
    {
        var invitation = await db.Invitations
            .Include(i => i.Wallet)
            .FirstOrDefaultAsync(i => i.WalletId == walletId && i.Code == code, ct);

        if (invitation is null || invitation.Wallet is null || invitation.Wallet.DeletedAt != null)
        {
            return Error.NotFound("Invitation not found.");
        }

        // Only the wallet Owner or the original inviter can revoke
        var role = await WalletAccessChecker.GetRoleAsync(db, walletId, requesterId, ct);
        var isOwner = role is not null && role.Value == WalletMemberRole.Owner;
        var isInviter = invitation.InviterUserId == requesterId;

        if (!isOwner && !isInviter)
        {
            return Error.Forbidden("You do not have permission to revoke this invitation.");
        }

        if (invitation.IsAccepted)
        {
            return Error.Conflict("This invitation has already been accepted and cannot be revoked.");
        }

        if (invitation.IsRevoked)
        {
            return Error.Conflict("This invitation has already been revoked.");
        }

        invitation.RevokedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return true;
    }
}
