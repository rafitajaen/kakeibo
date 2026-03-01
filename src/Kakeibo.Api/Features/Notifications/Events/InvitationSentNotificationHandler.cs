using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Infrastructure.WebPush;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.Events;

// Sends an invitation email and creates an in-app notification for the invitee when invited to a wallet.
public sealed class InvitationSentNotificationHandler(
    AppDbContext db,
    IEmailService emailService,
    IWebPushService pushService,
    ILogger<InvitationSentNotificationHandler> logger)
    : IEventHandler<InvitationSentEvent>
{
    public async Task HandleAsync(InvitationSentEvent @event, CancellationToken cancellationToken = default)
    {
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == @event.WalletId && w.DeletedAt == null, cancellationToken);

        if (wallet is null)
        {
            return;
        }

        var inviter = await db.Users.FirstOrDefaultAsync(u => u.Id == @event.InviterUserId, cancellationToken);
        if (inviter is null)
        {
            return;
        }

        // Load the invitation to get the token for the email link
        var invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Id == @event.InvitationId, cancellationToken);

        if (invitation is null)
        {
            return;
        }

        var title = "Wallet Invitation";
        var body = $"{inviter.Email} invited you to join the wallet \"{wallet.Name}\".";

        // Try to find the invitee user (may not exist yet — external invite)
        var inviteeUser = await db.Users.FirstOrDefaultAsync(u => u.Email == @event.InviteeEmail, cancellationToken);

        if (inviteeUser is not null)
        {
            // Create in-app notification for existing user
            db.Notifications.Add(new Notification
            {
                UserId = inviteeUser.Id,
                Type = NotificationTypes.InvitationSent,
                Title = title,
                Body = body,
                Metadata = $"{{\"invitationId\":\"{@event.InvitationId}\",\"walletId\":\"{@event.WalletId}\"}}"
            });
            await db.SaveChangesAsync(cancellationToken);

            var prefs = await db.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == inviteeUser.Id, cancellationToken)
                ?? new NotificationPreferences { UserId = inviteeUser.Id };

            if (prefs.PushInvitations)
            {
                try
                {
                    await pushService.SendAsync(inviteeUser.Id, title, body, cancellationToken);
                }
                catch (Exception ex)
                {
                    NotificationHandlerLogs.InvitationSentPushFailed(logger, inviteeUser.Id, ex);
                }
            }
        }

        // Always send the invitation email regardless of whether the user exists
        try
        {
            await emailService.SendWalletInvitationEmailAsync(
                @event.InvitationId,
                @event.InviteeEmail,
                wallet.Name,
                inviter.Email,
                invitation.Code,
                cancellationToken);
        }
        catch (Exception ex)
        {
            NotificationHandlerLogs.InvitationSentEmailFailed(logger, @event.InvitationId, ex);
        }
    }
}
