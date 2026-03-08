using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Friends.ListFriends;

// Lists all friends of the authenticated user.
public sealed class ListFriendsHandler(AppDbContext db)
{
    public async Task<List<ListFriendsEndpoint.ListFriendsResponse>> HandleAsync(
        Guid userId,
        CancellationToken ct)
    {
        // Friendships where the user is UserA
        var asUserA = db.Friendships
            .AsNoTracking()
            .Where(f => f.UserAId == userId)
            .Select(f => new ListFriendsEndpoint.ListFriendsResponse(
                f.Id,
                f.UserBId,
                f.UserB!.Username,
                f.UserB.Name,
                f.UserB.AvatarUrl,
                f.CreatedAt.ToString()));

        // Friendships where the user is UserB
        var asUserB = db.Friendships
            .AsNoTracking()
            .Where(f => f.UserBId == userId)
            .Select(f => new ListFriendsEndpoint.ListFriendsResponse(
                f.Id,
                f.UserAId,
                f.UserA!.Username,
                f.UserA.Name,
                f.UserA.AvatarUrl,
                f.CreatedAt.ToString()));

        // Materialize each side separately — EF Core cannot translate UNION after client projection
        var listA = await asUserA.ToListAsync(ct);
        var listB = await asUserB.ToListAsync(ct);

        return listA.Concat(listB).OrderBy(f => f.Username).ToList();
    }
}
