using NodaTime;

namespace Kakeibo.Api.Infrastructure.Events;

// Base interface for all in-process events dispatched via IEventBus
public interface IEvent
{
    Guid Id { get; }
    Instant OccurredAt { get; }
}
