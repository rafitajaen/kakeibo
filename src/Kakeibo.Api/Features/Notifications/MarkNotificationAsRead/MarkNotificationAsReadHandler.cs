using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.MarkNotificationAsRead;

// Marks a single notification as read for the authenticated user.
public sealed class MarkNotificationAsReadHandler(AppDbContext db)
{
    public async Task<Result<bool>> HandleAsync(Guid notificationId, Guid userId, CancellationToken ct)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification is null)
            return Error.NotFound($"Notification {notificationId} not found.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync(ct);
        }

        return true;
    }
}
