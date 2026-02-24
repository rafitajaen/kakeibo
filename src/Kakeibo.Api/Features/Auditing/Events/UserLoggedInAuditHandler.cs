using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Audit;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Api.Features.Auditing.Events;

// Records a user login event to the ClickHouse audit trail.
public sealed class UserLoggedInAuditHandler(IAuditService auditService)
    : IEventHandler<UserLoggedInEvent>
{
    public async Task HandleAsync(UserLoggedInEvent @event, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            UserId: @event.UserId,
            Action: AuditActions.UserLoggedIn,
            EntityType: "User",
            EntityId: @event.UserId,
            OccurredAt: @event.OccurredAt,
            IpAddress: @event.IpAddress,
            UserAgent: @event.UserAgent,
            Changes: null);

        await auditService.RecordAsync(entry, cancellationToken);
    }
}
