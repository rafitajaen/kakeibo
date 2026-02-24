using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Audit;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Api.Features.Auditing.Events;

// Records a wallet member-joined event to the ClickHouse audit trail.
public sealed class MemberJoinedAuditHandler(IAuditService auditService)
    : IEventHandler<MemberJoinedEvent>
{
    public async Task HandleAsync(MemberJoinedEvent @event, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            UserId: @event.UserId,
            Action: AuditActions.MemberJoined,
            EntityType: "Wallet",
            EntityId: @event.WalletId,
            OccurredAt: @event.OccurredAt,
            IpAddress: null,
            UserAgent: null,
            Changes: null);

        await auditService.RecordAsync(entry, cancellationToken);
    }
}
