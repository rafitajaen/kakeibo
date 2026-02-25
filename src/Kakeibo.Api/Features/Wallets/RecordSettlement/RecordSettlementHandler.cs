using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.RecordSettlement;

// Records an external debt settlement between two members of a shared wallet.
// Settlements do not affect wallet balance — money is transferred outside the platform.
public sealed class RecordSettlementHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<RecordSettlementEndpoint.RecordSettlementResponse>> HandleAsync(
        Guid walletId,
        RecordSettlementEndpoint.RecordSettlementRequest request,
        Guid userId,
        CancellationToken ct)
    {
        // Verify wallet exists and caller is a member
        var isMember = await db.WalletMembers
            .AnyAsync(wm => wm.WalletId == walletId && wm.UserId == userId, ct);

        if (!isMember)
        {
            var walletExists = await db.Wallets.AnyAsync(w => w.Id == walletId && w.DeletedAt == null, ct);
            return walletExists
                ? Error.Forbidden("You are not a member of this wallet.")
                : Error.NotFound($"Wallet {walletId} not found.");
        }

        // Verify the recipient is also a member
        var toIsMember = await db.WalletMembers
            .AnyAsync(wm => wm.WalletId == walletId && wm.UserId == request.ToUserId, ct);

        if (!toIsMember)
            return Error.Conflict("The recipient is not a member of this wallet.");

        var now = clock.GetCurrentInstant();

        var settlement = new Settlement
        {
            WalletId = walletId,
            FromUserId = userId,
            ToUserId = request.ToUserId,
            Amount = request.Amount,
            Notes = request.Notes
        };

        db.Settlements.Add(settlement);

        // Publish event before SaveChangesAsync — fire-and-forget via Channel
        eventBus.Publish(new SettlementRecordedEvent
        {
            SettlementId = settlement.Id,
            WalletId = walletId,
            FromUserId = userId,
            ToUserId = request.ToUserId,
            Amount = request.Amount,
            OccurredAt = now
        });

        await db.SaveChangesAsync(ct);

        return new RecordSettlementEndpoint.RecordSettlementResponse(
            settlement.Id,
            walletId,
            userId,
            request.ToUserId,
            request.Amount,
            request.Notes,
            now.ToString());
    }
}
