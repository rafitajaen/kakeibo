using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.Events;

public sealed record TransactionDeletedEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public required Guid TransactionId { get; init; }
    public required Guid WalletId { get; init; }
    public required string Type { get; init; }
    public required decimal Amount { get; init; }
    public required Guid UserId { get; init; }
    public required Guid CategoryId { get; init; }
    public required LocalDate Date { get; init; }
}
