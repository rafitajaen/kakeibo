using Kakeibo.Api.Features.Auditing;
using Kakeibo.Api.Features.Auditing.Events;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Audit;

namespace Kakeibo.Tests.Features.Auditing;

public sealed class InvitationAcceptedAuditHandlerTests
{
    [Fact]
    public async Task HandleAsync_InvitationAcceptedEvent_RecordsAuditEntryWithCorrectFields()
    {
        var auditService = Substitute.For<IAuditService>();
        var handler = new InvitationAcceptedAuditHandler(auditService);
        var invitationId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var occurredAt = Instant.FromUtc(2026, 3, 1, 12, 0);

        var @event = new InvitationAcceptedEvent
        {
            InvitationId = invitationId,
            WalletId = walletId,
            UserId = userId,
            OccurredAt = occurredAt
        };

        await handler.HandleAsync(@event, CancellationToken.None);

        await auditService.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.UserId == userId &&
                e.Action == AuditActions.InvitationAccepted &&
                e.EntityType == "Invitation" &&
                e.EntityId == invitationId &&
                e.OccurredAt == occurredAt &&
                e.IpAddress == null &&
                e.UserAgent == null &&
                e.Changes == null),
            Arg.Any<CancellationToken>());
    }
}
