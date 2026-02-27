using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.ArchiveWallet;

// Soft-deletes (archives) a wallet. Only the owner can archive.
public sealed class ArchiveWalletHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(
        Guid walletId,
        Guid userId,
        CancellationToken ct)
    {
        // Already-archived wallets are treated as not found
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.NotFound("Wallet not found.");
        }

        if (wallet.OwnerId != userId)
        {
            return Error.Forbidden("Only the owner can archive this wallet.");
        }

        wallet.DeletedAt = clock.GetCurrentInstant();
        wallet.UpdatedAt = clock.GetCurrentInstant();

        // Publish event before SaveChangesAsync — fire-and-forget via Channel
        eventBus.Publish(new WalletArchivedEvent
        {
            WalletId = wallet.Id,
            UserId = userId
        });

        await db.SaveChangesAsync(ct);

        return true;
    }
}
