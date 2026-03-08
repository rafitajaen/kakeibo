using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.MakeWalletPrivate;

// Converts a shared wallet to a personal wallet by removing all members except the owner.
// Exposes a two-step flow: PreviewAsync returns who will be removed; ExecuteAsync performs the conversion.
public sealed class MakeWalletPrivateHandler(AppDbContext db, IClock clock)
{
    // Returns a preview of the members who will lose access. Does not write to the DB.
    public async Task<Result<MakeWalletPrivateEndpoint.MakeWalletPrivatePreviewResponse>> PreviewAsync(
        Guid walletId,
        Guid callerId,
        CancellationToken ct)
    {
        var wallet = await db.Wallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.NotFound("Wallet not found.");
        }

        var callerRole = await WalletAccessChecker.GetRoleAsync(db, walletId, callerId, ct);
        if (callerRole != WalletMemberRole.Owner)
        {
            return Error.Forbidden("Only the wallet owner can convert it to private.");
        }

        // Collect the emails of non-owner members who will be removed
        var membersToRemove = await db.WalletMembers.AsNoTracking()
            .Where(m => m.WalletId == walletId && m.UserId != callerId)
            .Select(m => m.User!.Email)
            .ToListAsync(ct);

        return new MakeWalletPrivateEndpoint.MakeWalletPrivatePreviewResponse(
            membersToRemove,
            $"{membersToRemove.Count} member(s) will lose access to this wallet.");
    }

    // Converts the shared wallet to personal and removes all non-owner members.
    public async Task<Result<bool>> ExecuteAsync(
        Guid walletId,
        Guid callerId,
        CancellationToken ct)
    {
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.NotFound("Wallet not found.");
        }

        var callerRole = await WalletAccessChecker.GetRoleAsync(db, walletId, callerId, ct);
        if (callerRole != WalletMemberRole.Owner)
        {
            return Error.Forbidden("Only the wallet owner can convert it to private.");
        }

        // Remove all non-owner WalletMembers
        var membersToRemove = await db.WalletMembers
            .Where(m => m.WalletId == walletId && m.UserId != callerId)
            .ToListAsync(ct);

        db.WalletMembers.RemoveRange(membersToRemove);

        // Change type to Personal and set to private
        wallet.Type = WalletType.Personal;
        wallet.Visibility = WalletVisibility.Private;
        wallet.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return true;
    }
}
