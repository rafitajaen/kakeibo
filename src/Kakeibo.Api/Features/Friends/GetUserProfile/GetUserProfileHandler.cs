using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Friends.GetUserProfile;

// Returns a user's public profile with friendship status.
public sealed class GetUserProfileHandler(AppDbContext db)
{
    public async Task<Result<GetUserProfileEndpoint.GetUserProfileResponse>> HandleAsync(
        Guid profileUserId,
        Guid requestingUserId,
        CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == profileUserId)
            .Select(u => new { u.Id, u.Username, u.Name, u.AvatarUrl })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            return Error.NotFound("User not found.");
        }

        // Check friendship status
        var (userAId, userBId) = NormalizeIds(requestingUserId, profileUserId);
        var friendship = await db.Friendships
            .AsNoTracking()
            .Where(f => f.UserAId == userAId && f.UserBId == userBId)
            .Select(f => new { f.CreatedAt })
            .FirstOrDefaultAsync(ct);

        return new GetUserProfileEndpoint.GetUserProfileResponse(
            user.Id,
            user.Username,
            user.Name,
            user.AvatarUrl,
            friendship is not null,
            friendship?.CreatedAt.ToString());
    }

    private static (Guid, Guid) NormalizeIds(Guid a, Guid b)
        => a.CompareTo(b) < 0 ? (a, b) : (b, a);
}
