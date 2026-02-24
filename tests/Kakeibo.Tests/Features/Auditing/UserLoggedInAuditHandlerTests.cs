using Kakeibo.Api.Features.Auditing;
using Kakeibo.Api.Features.Auditing.Events;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Audit;

namespace Kakeibo.Tests.Features.Auditing;

public sealed class UserLoggedInAuditHandlerTests
{
    [Fact]
    public async Task HandleAsync_UserLoggedInEvent_RecordsAuditEntryWithCorrectFields()
    {
        var auditService = Substitute.For<IAuditService>();
        var handler = new UserLoggedInAuditHandler(auditService);
        var userId = Guid.NewGuid();
        var occurredAt = Instant.FromUtc(2026, 3, 1, 12, 0);

        var @event = new UserLoggedInEvent
        {
            UserId = userId,
            OccurredAt = occurredAt,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        await handler.HandleAsync(@event, CancellationToken.None);

        await auditService.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.UserId == userId &&
                e.Action == AuditActions.UserLoggedIn &&
                e.EntityType == "User" &&
                e.EntityId == userId &&
                e.OccurredAt == occurredAt &&
                e.IpAddress == "192.168.1.1" &&
                e.UserAgent == "Mozilla/5.0" &&
                e.Changes == null),
            Arg.Any<CancellationToken>());
    }
}
