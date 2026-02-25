using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.Events;

// Raised when a debt settlement is recorded between two wallet members.
public sealed record SettlementRecordedEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public required Guid SettlementId { get; init; }
    public required Guid WalletId { get; init; }
    public required Guid FromUserId { get; init; }
    public required Guid ToUserId { get; init; }
    public required decimal Amount { get; init; }
}
