using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Audit;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Api.Features.Auditing.Events;

// Records a transaction update event to the ClickHouse audit trail.
public sealed class TransactionUpdatedAuditHandler(IAuditService auditService)
    : IEventHandler<TransactionUpdatedEvent>
{
    public async Task HandleAsync(TransactionUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            UserId: @event.UserId,
            Action: AuditActions.TransactionUpdated,
            EntityType: "Transaction",
            EntityId: @event.TransactionId,
            OccurredAt: @event.OccurredAt,
            IpAddress: null,
            UserAgent: null,
            Changes: null);

        await auditService.RecordAsync(entry, cancellationToken);
    }
}
