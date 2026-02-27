using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.Events;

// Creates in-app notifications for both parties when a settlement is recorded.
// No email in MVP — settlement notifications are in-app only.
public sealed class SettlementRecordedNotificationHandler(AppDbContext db)
    : IEventHandler<SettlementRecordedEvent>
{
    public async Task HandleAsync(SettlementRecordedEvent @event, CancellationToken cancellationToken = default)
    {
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == @event.WalletId && w.DeletedAt == null, cancellationToken);

        if (wallet is null)
        {
            return;
        }

        var fromUser = await db.Users.FirstOrDefaultAsync(u => u.Id == @event.FromUserId, cancellationToken);
        var toUser = await db.Users.FirstOrDefaultAsync(u => u.Id == @event.ToUserId, cancellationToken);

        if (fromUser is null || toUser is null)
        {
            return;
        }

        // Notify the recipient of the payment
        db.Notifications.Add(new Notification
        {
            UserId = @event.ToUserId,
            Type = NotificationTypes.SettlementRecorded,
            Title = "Settlement Received",
            Body = $"{fromUser.Email} recorded a settlement of {@event.Amount:F2} to you in \"{wallet.Name}\".",
            Metadata = $"{{\"settlementId\":\"{@event.SettlementId}\",\"walletId\":\"{@event.WalletId}\",\"fromUserId\":\"{@event.FromUserId}\"}}"
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
