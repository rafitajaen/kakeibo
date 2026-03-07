using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Wallets.GetWallet;

// Returns a single wallet. Returns 404 if not found, 403 if the caller is not a member.
public sealed class GetWalletHandler(AppDbContext db)
{
    public async Task<Result<GetWalletEndpoint.GetWalletResponse>> HandleAsync(
        Guid walletId,
        Guid userId,
        CancellationToken ct)
    {
        var wallet = await db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.NotFound("Wallet not found.");
        }

        // Access check via WalletMember
        var role = await WalletAccessChecker.GetRoleAsync(db, walletId, userId, ct);
        if (role is null)
        {
            return Error.Forbidden("You do not have access to this wallet.");
        }

        // Read the current balance owned by the Transactions domain.
        var balance = await db.WalletBalances
            .AsNoTracking()
            .Where(wb => wb.WalletId == walletId)
            .Select(wb => wb.Balance)
            .FirstOrDefaultAsync(ct);

        return new GetWalletEndpoint.GetWalletResponse(
            wallet.Id,
            wallet.Name,
            wallet.Type.ToString(),
            wallet.Currency,
            Balance: balance,
            IsArchived: wallet.DeletedAt != null,
            Icon: wallet.Icon,
            BackgroundColor: wallet.BackgroundColor,
            TextColor: wallet.TextColor,
            wallet.CreatedAt,
            Visibility: wallet.Visibility.ToString(),
            CallerRole: role.Value.ToString());
    }
}
