using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Wallets.ListPublicWallets;

// Returns public wallets for a user, visible only to confirmed friends.
public sealed class ListPublicWalletsHandler(AppDbContext db)
{
    public async Task<Result<IReadOnlyList<ListPublicWalletsEndpoint.PublicWalletResponse>>> HandleAsync(
        Guid targetUserId,
        Guid callerId,
        CancellationToken ct)
    {
        // Cannot view your own public wallets via this endpoint — use ListWallets instead
        if (callerId == targetUserId)
        {
            return Error.Forbidden("Use the /api/wallets endpoint to view your own wallets.");
        }

        var targetExists = await db.Users.AnyAsync(u => u.Id == targetUserId, ct);
        if (!targetExists)
        {
            return Error.NotFound("User not found.");
        }

        // Caller must be a confirmed friend of the target user
        var (lo, hi) = callerId < targetUserId ? (callerId, targetUserId) : (targetUserId, callerId);
        var areFriends = await db.Friendships.AnyAsync(f => f.UserAId == lo && f.UserBId == hi, ct);
        if (!areFriends)
        {
            return Error.Forbidden("You must be friends to view public wallets.");
        }

        // Return non-archived public wallets where the target user is a member
        var wallets = await db.Wallets
            .AsNoTracking()
            .Where(w =>
                w.DeletedAt == null &&
                w.Visibility == WalletVisibility.Public &&
                w.WalletMembers.Any(m => m.UserId == targetUserId))
            .OrderBy(w => w.CreatedAt)
            .Select(w => new ListPublicWalletsEndpoint.PublicWalletResponse(
                w.Id,
                w.Name,
                w.Type.ToString(),
                w.Currency,
                w.CreatedAt))
            .ToListAsync(ct);

        return wallets;
    }
}
