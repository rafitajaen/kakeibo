using Kakeibo.Api.Features.Auditing;
using Kakeibo.Api.Features.Auditing.Events;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Audit;

namespace Kakeibo.Tests.Features.Auditing;

public sealed class InvitationSentAuditHandlerTests
{
    [Fact]
    public async Task HandleAsync_InvitationSentEvent_RecordsAuditEntryWithCorrectFields()
    {
        var auditService = Substitute.For<IAuditService>();
        var handler = new InvitationSentAuditHandler(auditService);
        var invitationId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var inviterUserId = Guid.NewGuid();
        var occurredAt = Instant.FromUtc(2026, 3, 1, 12, 0);

        var @event = new InvitationSentEvent
        {
            InvitationId = invitationId,
            WalletId = walletId,
            InviterUserId = inviterUserId,
            InviteeEmail = "invitee@example.com",
            OccurredAt = occurredAt
        };

        await handler.HandleAsync(@event, CancellationToken.None);

        await auditService.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.UserId == inviterUserId &&
                e.Action == AuditActions.InvitationSent &&
                e.EntityType == "Invitation" &&
                e.EntityId == invitationId &&
                e.OccurredAt == occurredAt &&
                e.IpAddress == null &&
                e.UserAgent == null &&
                e.Changes == null),
            Arg.Any<CancellationToken>());
    }
}
