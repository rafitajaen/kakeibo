using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Audit;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Api.Features.Auditing.Events;

// Records a wallet invitation-sent event to the ClickHouse audit trail.
public sealed class InvitationSentAuditHandler(IAuditService auditService)
    : IEventHandler<InvitationSentEvent>
{
    public async Task HandleAsync(InvitationSentEvent @event, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            UserId: @event.InviterUserId,
            Action: AuditActions.InvitationSent,
            EntityType: "Invitation",
            EntityId: @event.InvitationId,
            OccurredAt: @event.OccurredAt,
            IpAddress: null,
            UserAgent: null,
            Changes: null);

        await auditService.RecordAsync(entry, cancellationToken);
    }
}
