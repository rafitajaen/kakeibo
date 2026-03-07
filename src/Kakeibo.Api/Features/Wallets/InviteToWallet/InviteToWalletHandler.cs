using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.InviteToWallet;

// Sends an invitation to join a shared wallet. Only Owner can invite.
public sealed class InviteToWalletHandler(
    AppDbContext db,
    IEventBus eventBus,
    IEmailService emailService,
    IClock clock,
    ILogger<InviteToWalletHandler> logger)
{
    // Max pending invitations per wallet to prevent spam
    private const int MaxPendingInvitations = 50;

    public async Task<Result<InviteToWalletEndpoint.InviteToWalletResponse>> HandleAsync(
        InviteToWalletEndpoint.InviteToWalletRequest request,
        Guid walletId,
        Guid requesterId,
        CancellationToken ct)
    {
        var now = clock.GetCurrentInstant();

        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.Id == walletId, ct);
        if (wallet is null)
        {
            return Error.NotFound("Wallet not found.");
        }

        // Only shared wallets support invitations
        if (wallet.Type != WalletType.Shared)
        {
            return Error.Forbidden("Only shared wallets can have invitations.");
        }

        // Only Owner can invite
        var role = await WalletAccessChecker.GetRoleAsync(db, walletId, requesterId, ct);
        if (role is null || role.Value != WalletMemberRole.Owner)
        {
            return Error.Forbidden("Only the wallet owner can send invitations.");
        }

        // Check invitee is not already a member
        var inviteeUser = await db.Users.FirstOrDefaultAsync(
            u => u.Email == request.InviteeEmail, ct);

        if (inviteeUser is not null)
        {
            var alreadyMember = await db.WalletMembers.AnyAsync(
                m => m.WalletId == walletId && m.UserId == inviteeUser.Id, ct);

            if (alreadyMember)
            {
                return Error.Conflict($"'{request.InviteeEmail}' is already a member of this wallet.");
            }

            // Inviting an existing user requires a confirmed friendship
            var (lo, hi) = requesterId < inviteeUser.Id
                ? (requesterId, inviteeUser.Id)
                : (inviteeUser.Id, requesterId);

            var areFriends = await db.Friendships.AnyAsync(
                f => f.UserAId == lo && f.UserBId == hi, ct);

            if (!areFriends)
            {
                return Error.Forbidden("You can only invite friends to a wallet.");
            }
        }

        // Check no active (non-expired, non-revoked, non-accepted) invitation exists for this email
        var hasPending = await db.Invitations.AnyAsync(i =>
            i.WalletId == walletId &&
            i.InviteeEmail == request.InviteeEmail &&
            i.AcceptedAt == null &&
            i.RevokedAt == null &&
            i.ExpiresAt > now, ct);

        if (hasPending)
        {
            return Error.Conflict($"An active invitation for '{request.InviteeEmail}' already exists.");
        }

        // Enforce max pending invitations cap
        var pendingCount = await db.Invitations.CountAsync(i =>
            i.WalletId == walletId &&
            i.AcceptedAt == null &&
            i.RevokedAt == null &&
            i.ExpiresAt > now, ct);

        if (pendingCount >= MaxPendingInvitations)
        {
            return Error.Conflict("This wallet has reached the maximum number of pending invitations.");
        }

        var invitation = new Invitation
        {
            WalletId = walletId,
            InviterUserId = requesterId,
            InviteeEmail = request.InviteeEmail,
            Code = RandomString.Generate(32, CharSets.AlphanumericSafe),
            ExpiresAt = now.Plus(Duration.FromDays(7))
        };

        db.Invitations.Add(invitation);

        eventBus.Publish(new InvitationSentEvent
        {
            InvitationId = invitation.Id,
            WalletId = walletId,
            InviterUserId = requesterId,
            InviteeEmail = request.InviteeEmail
        });

        await db.SaveChangesAsync(ct);

        // Send invitation email fire-and-forget — do not await to avoid blocking the response.
        // Failures are logged but do not roll back the invitation (it exists in DB with a valid code).
        var inviterUser = await db.Users.FirstOrDefaultAsync(u => u.Id == requesterId, ct);
        _ = emailService.SendWalletInvitationEmailAsync(
                invitation.Id,
                request.InviteeEmail,
                wallet.Name,
                inviterUser?.Email ?? requesterId.ToString(),
                invitation.Code,
                CancellationToken.None)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    logger.InvitationEmailFailed(request.InviteeEmail, walletId, t.Exception!);
                }
            }, TaskScheduler.Default);

        return new InviteToWalletEndpoint.InviteToWalletResponse(
            invitation.Id,
            invitation.Code,
            invitation.InviteeEmail);
    }
}
