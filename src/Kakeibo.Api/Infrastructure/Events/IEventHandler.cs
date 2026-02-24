namespace Kakeibo.Api.Infrastructure.Events;

// Handler for a specific event type. Registered in DI and invoked by EventDispatcher.
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
