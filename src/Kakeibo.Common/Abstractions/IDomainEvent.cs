using NodaTime;

namespace Kakeibo.Common.Abstractions;

// Internal module event - dispatched synchronously during SaveChangesAsync
public interface IDomainEvent
{
    Guid Id { get; init; }
    Instant OccurredAt { get; init; }
}
