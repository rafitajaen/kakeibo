using Kakeibo.Common.Abstractions;

namespace Kakeibo.Common.Modules;

// Consumer for integration events - transport-agnostic
public interface IEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task ConsumeAsync(TEvent @event, CancellationToken cancellationToken);
}
