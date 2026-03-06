using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.UpdateWallet;

// Updates a wallet's name and visual customization. Only the owner can update. Type and currency are immutable.
public sealed class UpdateWalletHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdateWalletEndpoint.UpdateWalletResponse>> HandleAsync(
        Guid walletId,
        UpdateWalletEndpoint.UpdateWalletRequest request,
        Guid userId,
        CancellationToken ct)
    {
        // Archived wallets are treated as not found for update operations
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.NotFound("Wallet not found.");
        }

        if (wallet.OwnerId != userId)
        {
            return Error.Forbidden("Only the owner can update this wallet.");
        }

        // Check for duplicate name among the owner's active wallets (excluding current)
        var nameConflict = await db.Wallets
            .AnyAsync(w => w.OwnerId == userId
                        && w.Name == request.Name
                        && w.DeletedAt == null
                        && w.Id != walletId, ct);
        if (nameConflict)
        {
            return Error.Conflict($"A wallet named '{request.Name}' already exists.");
        }

        wallet.Name = request.Name;
        wallet.Icon = request.Icon;
        wallet.BackgroundColor = request.BackgroundColor;
        wallet.TextColor = request.TextColor;
        wallet.UpdatedAt = clock.GetCurrentInstant();

        // Read current balance for the response
        var balance = await db.WalletBalances
            .Where(wb => wb.WalletId == walletId)
            .Select(wb => wb.Balance)
            .FirstOrDefaultAsync(ct);

        await db.SaveChangesAsync(ct);

        return new UpdateWalletEndpoint.UpdateWalletResponse(
            wallet.Id,
            wallet.Name,
            wallet.Type.ToString(),
            wallet.Currency,
            Balance: balance,
            IsArchived: false,
            Icon: wallet.Icon,
            BackgroundColor: wallet.BackgroundColor,
            TextColor: wallet.TextColor,
            wallet.CreatedAt);
    }
}
