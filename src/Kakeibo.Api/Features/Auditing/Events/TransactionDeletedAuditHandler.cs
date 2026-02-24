using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Audit;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Api.Features.Auditing.Events;

// Records a transaction deletion event to the ClickHouse audit trail.
public sealed class TransactionDeletedAuditHandler(IAuditService auditService)
    : IEventHandler<TransactionDeletedEvent>
{
    public async Task HandleAsync(TransactionDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            UserId: @event.UserId,
            Action: AuditActions.TransactionDeleted,
            EntityType: "Transaction",
            EntityId: @event.TransactionId,
            OccurredAt: @event.OccurredAt,
            IpAddress: null,
            UserAgent: null,
            Changes: null);

        await auditService.RecordAsync(entry, cancellationToken);
    }
}
