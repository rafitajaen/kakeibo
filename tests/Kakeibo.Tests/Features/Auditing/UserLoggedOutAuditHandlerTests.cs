using Kakeibo.Api.Features.Auditing;
using Kakeibo.Api.Features.Auditing.Events;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Audit;

namespace Kakeibo.Tests.Features.Auditing;

public sealed class UserLoggedOutAuditHandlerTests
{
    [Fact]
    public async Task HandleAsync_SingleSessionLogout_RecordsUserLoggedOutAction()
    {
        var auditService = Substitute.For<IAuditService>();
        var handler = new UserLoggedOutAuditHandler(auditService);
        var userId = Guid.NewGuid();
        var occurredAt = Instant.FromUtc(2026, 3, 1, 12, 0);

        var @event = new UserLoggedOutEvent
        {
            UserId = userId,
            OccurredAt = occurredAt,
            AllSessions = false
        };

        await handler.HandleAsync(@event, CancellationToken.None);

        await auditService.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.UserId == userId &&
                e.Action == AuditActions.UserLoggedOut &&
                e.EntityType == "User" &&
                e.EntityId == userId &&
                e.OccurredAt == occurredAt &&
                e.Changes == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AllSessionsLogout_RecordsUserLoggedOutAllSessionsAction()
    {
        var auditService = Substitute.For<IAuditService>();
        var handler = new UserLoggedOutAuditHandler(auditService);
        var userId = Guid.NewGuid();

        var @event = new UserLoggedOutEvent
        {
            UserId = userId,
            AllSessions = true
        };

        await handler.HandleAsync(@event, CancellationToken.None);

        await auditService.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.UserId == userId &&
                e.Action == AuditActions.UserLoggedOutAllSessions),
            Arg.Any<CancellationToken>());
    }
}
