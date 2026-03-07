using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Wallets;

// Checks wallet membership and returns the member's role, or null if not a member.
internal static class WalletAccessChecker
{
    // Returns the user's role in the wallet, or null if they are not a member.
    internal static async Task<WalletMemberRole?> GetRoleAsync(
        AppDbContext db, Guid walletId, Guid userId, CancellationToken ct)
    {
        return await db.WalletMembers
            .Where(m => m.WalletId == walletId && m.UserId == userId)
            .Select(m => (WalletMemberRole?)m.Role)
            .FirstOrDefaultAsync(ct);
    }

    // Returns true if the user has at least the specified role in the wallet.
    internal static async Task<bool> HasRoleAsync(
        AppDbContext db, Guid walletId, Guid userId, WalletMemberRole minimumRole, CancellationToken ct)
    {
        var role = await GetRoleAsync(db, walletId, userId, ct);
        return role is not null && role.Value <= minimumRole;
    }
}
