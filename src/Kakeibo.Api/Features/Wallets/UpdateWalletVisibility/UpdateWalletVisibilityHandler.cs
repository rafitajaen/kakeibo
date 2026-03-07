using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.UpdateWalletVisibility;

// Updates the visibility (Private/Public) of a wallet. Only the Owner can change it.
public sealed class UpdateWalletVisibilityHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(
        Guid walletId,
        UpdateWalletVisibilityEndpoint.UpdateWalletVisibilityRequest request,
        Guid userId,
        CancellationToken ct)
    {
        if (!Enum.TryParse<WalletVisibility>(request.Visibility, ignoreCase: true, out var visibility))
        {
            return Error.Validation("Invalid visibility value. Must be 'Private' or 'Public'.");
        }

        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);
        if (wallet is null)
        {
            return Error.NotFound("Wallet not found.");
        }

        // Only Owner can change visibility
        var role = await WalletAccessChecker.GetRoleAsync(db, walletId, userId, ct);
        if (role is null || role.Value != WalletMemberRole.Owner)
        {
            return Error.Forbidden("Only the wallet owner can change visibility.");
        }

        wallet.Visibility = visibility;
        wallet.UpdatedAt = clock.GetCurrentInstant();
        await db.SaveChangesAsync(ct);

        return true;
    }
}
