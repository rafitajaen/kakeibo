using Kakeibo.Api.Features.Auditing;
using Kakeibo.Api.Features.Auditing.Events;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Audit;

namespace Kakeibo.Tests.Features.Auditing;

public sealed class UserRegisteredAuditHandlerTests
{
    [Fact]
    public async Task HandleAsync_UserRegisteredEvent_RecordsAuditEntryWithCorrectFields()
    {
        var auditService = Substitute.For<IAuditService>();
        var handler = new UserRegisteredAuditHandler(auditService);
        var userId = Guid.NewGuid();
        var occurredAt = Instant.FromUtc(2026, 3, 1, 12, 0);

        var @event = new UserRegisteredEvent
        {
            UserId = userId,
            OccurredAt = occurredAt,
            Email = "alice@example.com"
        };

        await handler.HandleAsync(@event, CancellationToken.None);

        await auditService.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.UserId == userId &&
                e.Action == AuditActions.UserRegistered &&
                e.EntityType == "User" &&
                e.EntityId == userId &&
                e.OccurredAt == occurredAt &&
                e.IpAddress == null &&
                e.UserAgent == null &&
                e.Changes == null),
            Arg.Any<CancellationToken>());
    }
}
