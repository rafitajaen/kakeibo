using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.UpdateWalletMemberRole;

// Updates the role of a wallet member. Only the Owner can change roles.
// The Owner role itself cannot be assigned via this endpoint (use transfer instead).
public sealed class UpdateWalletMemberRoleHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(
        Guid walletId,
        Guid targetUserId,
        string roleName,
        Guid callerId,
        CancellationToken ct)
    {
        // Validate the requested role — Owner cannot be assigned via this endpoint
        if (!Enum.TryParse<WalletMemberRole>(roleName, ignoreCase: true, out var newRole)
            || newRole == WalletMemberRole.Owner)
        {
            return Error.Validation("Role must be 'Editor' or 'Guest'.");
        }

        // Caller must be Owner to change roles
        var callerRole = await WalletAccessChecker.GetRoleAsync(db, walletId, callerId, ct);
        if (callerRole != WalletMemberRole.Owner)
        {
            return Error.Forbidden("Only the wallet owner can change member roles.");
        }

        // Cannot change the caller's own role (they are the Owner)
        if (targetUserId == callerId)
        {
            return Error.Validation("You cannot change your own role.");
        }

        var member = await db.WalletMembers
            .FirstOrDefaultAsync(m => m.WalletId == walletId && m.UserId == targetUserId, ct);

        if (member is null)
        {
            return Error.NotFound("Member not found in this wallet.");
        }

        // Owners cannot be demoted via this endpoint — use transfer instead
        if (member.Role == WalletMemberRole.Owner)
        {
            return Error.Validation("The wallet owner's role cannot be changed via this endpoint.");
        }

        member.Role = newRole;
        member.UpdatedAt = clock.GetCurrentInstant();
        await db.SaveChangesAsync(ct);

        return true;
    }
}
