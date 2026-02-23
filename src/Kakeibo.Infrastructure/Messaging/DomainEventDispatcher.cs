using Kakeibo.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kakeibo.Infrastructure.Messaging;

// Resolves all IDomainEventHandler<T> for a given domain event via DI and invokes sequentially.
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider)
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        // Resolve all handlers for this event type (may be zero, one, or multiple)
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))
                ?? throw new InvalidOperationException($"HandleAsync method not found on handler type '{handlerType.Name}'.");

            var task = method.Invoke(handler, [domainEvent, cancellationToken]) as Task
                ?? throw new InvalidOperationException($"HandleAsync did not return a Task.");

            await task;
        }
    }
}
