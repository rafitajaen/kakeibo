using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kakeibo.Api.Infrastructure.Events;

// Background service that drains the ChannelEventBus and dispatches events to IEventHandler<T>.
// Each event is dispatched within a fresh DI scope so handlers get scoped dependencies.
public sealed class EventDispatcher(
    ChannelEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<EventDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Read events continuously until the host shuts down
        await foreach (var @event in eventBus.Reader.ReadAllAsync(stoppingToken))
        {
            await DispatchEventAsync(@event, stoppingToken);
        }
    }

    // Resolves all IEventHandler<TEvent> from a new scope and invokes them sequentially.
    private async Task DispatchEventAsync(IEvent @event, CancellationToken cancellationToken)
    {
        var eventType = @event.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);

        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            try
            {
                var method = handlerType.GetMethod(nameof(IEventHandler<IEvent>.HandleAsync))!;
                var task = (Task)(method.Invoke(handler, [@event, cancellationToken])
                    ?? throw new InvalidOperationException(
                        $"HandleAsync returned null for handler '{handler?.GetType().Name}'."));
                await task;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error dispatching event {EventType} to handler {HandlerType}",
                    eventType.Name, handler?.GetType().Name);
            }
        }
    }
}
