using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Friends.ListReceivedRequests;

// Lists pending friend requests received by the authenticated user.
public sealed class ListReceivedRequestsHandler(AppDbContext db)
{
    public async Task<List<ListReceivedRequestsEndpoint.ListReceivedRequestsResponse>> HandleAsync(
        Guid userId,
        CancellationToken ct)
    {
        return await db.FriendRequests
            .Where(r => r.ReceiverUserId == userId && r.AcceptedAt == null && r.RejectedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ListReceivedRequestsEndpoint.ListReceivedRequestsResponse(
                r.Id,
                r.SenderUserId,
                r.Sender!.Username,
                r.Sender.Name,
                r.Sender.AvatarUrl,
                r.CreatedAt.ToString()))
            .ToListAsync(ct);
    }
}
