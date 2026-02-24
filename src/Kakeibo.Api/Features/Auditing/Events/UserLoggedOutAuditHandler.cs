using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Audit;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Api.Features.Auditing.Events;

// Records a user logout event to the ClickHouse audit trail.
public sealed class UserLoggedOutAuditHandler(IAuditService auditService)
    : IEventHandler<UserLoggedOutEvent>
{
    public async Task HandleAsync(UserLoggedOutEvent @event, CancellationToken cancellationToken = default)
    {
        var action = @event.AllSessions
            ? AuditActions.UserLoggedOutAllSessions
            : AuditActions.UserLoggedOut;

        var entry = new AuditEntry(
            UserId: @event.UserId,
            Action: action,
            EntityType: "User",
            EntityId: @event.UserId,
            OccurredAt: @event.OccurredAt,
            IpAddress: null,
            UserAgent: null,
            Changes: null);

        await auditService.RecordAsync(entry, cancellationToken);
    }
}
