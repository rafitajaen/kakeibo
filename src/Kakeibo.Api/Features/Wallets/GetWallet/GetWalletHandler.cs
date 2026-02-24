using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Wallets.GetWallet;

// Returns a single wallet. Returns 404 if not found, 403 if the caller is not the owner.
public sealed class GetWalletHandler(AppDbContext db)
{
    public async Task<Result<GetWalletEndpoint.GetWalletResponse>> HandleAsync(
        Guid walletId,
        Guid userId,
        CancellationToken ct)
    {
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
            return Error.NotFound("Wallet not found.");

        // Allow owner or any WalletMember to access
        var isOwner = wallet.OwnerId == userId;
        var isMember = await db.WalletMembers
            .AnyAsync(m => m.WalletId == walletId && m.UserId == userId, ct);

        if (!isOwner && !isMember)
            return Error.Forbidden("You do not have access to this wallet.");

        return new GetWalletEndpoint.GetWalletResponse(
            wallet.Id,
            wallet.Name,
            wallet.Type.ToString(),
            wallet.Currency,
            Balance: 0m,
            IsArchived: wallet.DeletedAt != null,
            wallet.CreatedAt);
    }
}
