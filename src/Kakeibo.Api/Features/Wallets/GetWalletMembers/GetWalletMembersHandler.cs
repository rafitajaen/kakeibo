using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Wallets.GetWalletMembers;

// Returns all members of a wallet with their roles. Access: any member.
public sealed class GetWalletMembersHandler(AppDbContext db)
{
    public async Task<Result<IReadOnlyList<GetWalletMembersEndpoint.MemberDto>>> HandleAsync(
        Guid walletId,
        Guid requesterId,
        CancellationToken ct)
    {
        var walletExists = await db.Wallets.AnyAsync(w => w.Id == walletId, ct);
        if (!walletExists)
        {
            return Error.NotFound("Wallet not found.");
        }

        // Access check via WalletMember
        var role = await WalletAccessChecker.GetRoleAsync(db, walletId, requesterId, ct);
        if (role is null)
        {
            return Error.Forbidden("You are not a member of this wallet.");
        }

        // Build member list from WalletMembers — Owner first, then by join date
        var members = await db.WalletMembers
            .AsNoTracking()
            .Where(m => m.WalletId == walletId)
            .Include(m => m.User)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.CreatedAt)
            .Select(m => new GetWalletMembersEndpoint.MemberDto(
                m.UserId,
                m.User!.Email,
                Role: m.Role.ToString(),
                JoinedAt: m.CreatedAt))
            .ToListAsync(ct);

        return members;
    }
}
