using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Friends.ListSentRequests;

// Lists pending friend requests sent by the authenticated user.
public sealed class ListSentRequestsHandler(AppDbContext db)
{
    public async Task<List<ListSentRequestsEndpoint.ListSentRequestsResponse>> HandleAsync(
        Guid userId,
        CancellationToken ct)
    {
        return await db.FriendRequests
            .AsNoTracking()
            .Where(r => r.SenderUserId == userId && r.AcceptedAt == null && r.RejectedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ListSentRequestsEndpoint.ListSentRequestsResponse(
                r.Id,
                r.ReceiverUserId,
                r.Receiver!.Username,
                r.Receiver.Name,
                r.Receiver.AvatarUrl,
                r.CreatedAt.ToString()))
            .ToListAsync(ct);
    }
}
