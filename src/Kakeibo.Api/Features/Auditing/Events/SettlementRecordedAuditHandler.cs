using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Audit;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Api.Features.Auditing.Events;

// Records a settlement-recorded event to the ClickHouse audit trail.
public sealed class SettlementRecordedAuditHandler(IAuditService auditService)
    : IEventHandler<SettlementRecordedEvent>
{
    public async Task HandleAsync(SettlementRecordedEvent @event, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            UserId: @event.FromUserId,
            Action: AuditActions.SettlementRecorded,
            EntityType: "Wallet",
            EntityId: @event.WalletId,
            OccurredAt: @event.OccurredAt,
            IpAddress: null,
            UserAgent: null,
            Changes: null);

        await auditService.RecordAsync(entry, cancellationToken);
    }
}
