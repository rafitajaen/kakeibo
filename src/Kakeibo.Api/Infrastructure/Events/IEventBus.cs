namespace Kakeibo.Api.Infrastructure.Events;

// Publishes events for async in-process delivery via System.Threading.Channels.
// Fire-and-forget: caller does not wait for handlers to complete.
public interface IEventBus
{
    void Publish<TEvent>(TEvent @event) where TEvent : IEvent;
}
