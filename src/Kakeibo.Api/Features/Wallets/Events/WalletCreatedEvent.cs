using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.Events;

public sealed record WalletCreatedEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public required Guid WalletId { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
}
