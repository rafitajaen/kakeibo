using System.Threading.Channels;

namespace Kakeibo.Api.Infrastructure.Events;

// In-memory event bus backed by an unbounded channel.
// Singleton: shared across all requests so the EventDispatcher background service can drain it.
public sealed class ChannelEventBus : IEventBus
{
    private readonly Channel<IEvent> _channel = Channel.CreateUnbounded<IEvent>(
        new UnboundedChannelOptions
        {
            // Single reader (EventDispatcher background service)
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

    // Exposed to EventDispatcher so it can read events from the channel.
    internal ChannelReader<IEvent> Reader => _channel.Reader;

    // Enqueues an event for async delivery. Never blocks the caller.
    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        _channel.Writer.TryWrite(@event);
    }
}
