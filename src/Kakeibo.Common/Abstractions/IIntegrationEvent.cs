using NodaTime;

namespace Kakeibo.Common.Abstractions;

// Cross-module event - persisted in outbox and dispatched asynchronously
public interface IIntegrationEvent
{
    Guid Id { get; init; }
    Instant OccurredAt { get; init; }
    int Version { get; }
}
