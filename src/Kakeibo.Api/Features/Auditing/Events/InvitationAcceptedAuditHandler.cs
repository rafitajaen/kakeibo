using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Audit;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Api.Features.Auditing.Events;

// Records a wallet invitation-accepted event to the ClickHouse audit trail.
public sealed class InvitationAcceptedAuditHandler(IAuditService auditService)
    : IEventHandler<InvitationAcceptedEvent>
{
    public async Task HandleAsync(InvitationAcceptedEvent @event, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            UserId: @event.UserId,
            Action: AuditActions.InvitationAccepted,
            EntityType: "Invitation",
            EntityId: @event.InvitationId,
            OccurredAt: @event.OccurredAt,
            IpAddress: null,
            UserAgent: null,
            Changes: null);

        await auditService.RecordAsync(entry, cancellationToken);
    }
}
