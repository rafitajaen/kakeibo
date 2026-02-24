using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.Events;

public sealed record InvitationSentEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public required Guid InvitationId { get; init; }
    public required Guid WalletId { get; init; }
    public required Guid InviterUserId { get; init; }
    public required string InviteeEmail { get; init; }
}
