using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.ListNotifications;

// Returns paginated in-app notifications for the current user, newest first.
public sealed class ListNotificationsHandler(AppDbContext db)
{
    public async Task<ListNotificationsEndpoint.ListNotificationsResponse> HandleAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var maxPageSize = 50;
        // Clamp page size to prevent excessive results
        var clampedSize = Math.Min(pageSize, maxPageSize);
        var offset = (Math.Max(page, 1) - 1) * clampedSize;

        var unreadCount = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync(ct);

        var items = await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(offset)
            .Take(clampedSize)
            .Select(n => new ListNotificationsEndpoint.NotificationItem(
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.IsRead,
                n.CreatedAt.ToString()))
            .ToListAsync(ct);

        return new ListNotificationsEndpoint.ListNotificationsResponse(unreadCount, items);
    }
}
