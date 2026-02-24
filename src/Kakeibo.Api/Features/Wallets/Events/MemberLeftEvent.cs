using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.Events;

public sealed record MemberLeftEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public required Guid WalletId { get; init; }
    public required Guid UserId { get; init; }
}
