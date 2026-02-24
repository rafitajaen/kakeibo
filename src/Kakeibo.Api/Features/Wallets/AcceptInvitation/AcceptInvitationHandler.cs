using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Npgsql;

namespace Kakeibo.Api.Features.Wallets.AcceptInvitation;

// Accepts a wallet invitation by code. Creates a WalletMember and marks the invitation accepted.
public sealed class AcceptInvitationHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<AcceptInvitationEndpoint.AcceptInvitationResponse>> HandleAsync(
        string code,
        Guid userId,
        CancellationToken ct)
    {
        var now = clock.GetCurrentInstant();

        var invitation = await db.Invitations
            .Include(i => i.Wallet)
            .FirstOrDefaultAsync(i => i.Code == code, ct);

        if (invitation is null)
            return Error.NotFound("Invitation not found.");

        if (invitation.IsRevoked)
            return Error.Validation("This invitation has been revoked.");

        // Use ExpiresAt directly — not the computed IsExpired property (TD-016 pattern)
        if (invitation.ExpiresAt <= now)
            return Error.Validation("This invitation has expired.");

        if (invitation.IsAccepted)
            return Error.Conflict("This invitation has already been accepted.");

        // Check requester is not already a member
        var alreadyMember = invitation.Wallet!.OwnerId == userId ||
            await db.WalletMembers.AnyAsync(
                m => m.WalletId == invitation.WalletId && m.UserId == userId, ct);

        if (alreadyMember)
            return Error.Conflict("You are already a member of this wallet.");

        // Mark invitation accepted and create WalletMember in the same transaction
        invitation.AcceptedAt = now;

        var member = new WalletMember
        {
            WalletId = invitation.WalletId,
            UserId = userId
        };
        db.WalletMembers.Add(member);

        eventBus.Publish(new InvitationAcceptedEvent
        {
            InvitationId = invitation.Id,
            WalletId = invitation.WalletId,
            UserId = userId
        });

        eventBus.Publish(new MemberJoinedEvent
        {
            WalletId = invitation.WalletId,
            UserId = userId
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Unique constraint violation — two concurrent requests accepted the same invitation
            return Error.Conflict("You are already a member of this wallet.");
        }

        return new AcceptInvitationEndpoint.AcceptInvitationResponse(
            invitation.WalletId,
            invitation.Wallet.Name);
    }
}
