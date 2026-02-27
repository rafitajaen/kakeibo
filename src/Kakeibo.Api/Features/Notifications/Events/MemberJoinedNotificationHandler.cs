using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Infrastructure.WebPush;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.Events;

// Notifies all existing wallet members when a new member joins a shared wallet.
public sealed class MemberJoinedNotificationHandler(
    AppDbContext db,
    IEmailService emailService,
    IWebPushService pushService,
    ILogger<MemberJoinedNotificationHandler> logger)
    : IEventHandler<MemberJoinedEvent>
{
    public async Task HandleAsync(MemberJoinedEvent @event, CancellationToken cancellationToken = default)
    {
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == @event.WalletId && w.DeletedAt == null, cancellationToken);

        if (wallet is null)
        {
            return;
        }

        var newMember = await db.Users.FirstOrDefaultAsync(u => u.Id == @event.UserId, cancellationToken);
        if (newMember is null)
        {
            return;
        }

        // Notify all other members of the wallet
        var otherMemberIds = await db.WalletMembers
            .Where(wm => wm.WalletId == @event.WalletId && wm.UserId != @event.UserId)
            .Select(wm => wm.UserId)
            .ToListAsync(cancellationToken);

        var title = "New Member Joined";
        var body = $"{newMember.Email} joined the wallet \"{wallet.Name}\".";

        foreach (var memberId in otherMemberIds)
        {
            var member = await db.Users.FirstOrDefaultAsync(u => u.Id == memberId, cancellationToken);
            if (member is null)
            {
                continue;
            }

            // Create in-app notification
            db.Notifications.Add(new Notification
            {
                UserId = memberId,
                Type = NotificationTypes.MemberJoined,
                Title = title,
                Body = body,
                Metadata = $"{{\"walletId\":\"{@event.WalletId}\",\"newMemberId\":\"{@event.UserId}\"}}"
            });

            var prefs = await db.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == memberId, cancellationToken)
                ?? new NotificationPreferences { UserId = memberId };

            if (prefs.EmailInvitations)
            {
                try
                {
                    await emailService.SendMemberJoinedEmailAsync(
                        memberId, member.Email, wallet.Name, newMember.Email, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send member joined email to user {UserId}", memberId);
                }
            }

            if (prefs.PushInvitations)
            {
                try
                {
                    await pushService.SendAsync(memberId, title, body, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send member joined push to user {UserId}", memberId);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
