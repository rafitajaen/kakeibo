using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Recurring.Events;

// Published after the Hangfire job successfully generates a transaction from a recurring pattern.
// Consumed by Notifications in a future phase.
public sealed record RecurringTransactionGeneratedEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; }
    public required Guid RecurringPatternId { get; init; }
    public required Guid TransactionId { get; init; }
    public required Guid UserId { get; init; }
}
