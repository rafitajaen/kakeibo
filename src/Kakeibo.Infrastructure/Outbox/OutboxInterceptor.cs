using Kakeibo.Common.Abstractions;
using Kakeibo.Common.Modules;
using Kakeibo.Common.Persistence;
using Kakeibo.Common.Utils;
using Kakeibo.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NodaTime;

namespace Kakeibo.Infrastructure.Outbox;

// SaveChangesInterceptor that:
// 1. Harvests domain events from ChangeTracker.Entries<Entity>()
// 2. Dispatches them via DomainEventDispatcher (handlers publish integration events + stage audit)
// 3. Reads buffered integration events from ModuleEventBus
// 4. Writes OutboxMessage rows within the same database transaction (atomic)
public sealed class OutboxInterceptor(
    DomainEventDispatcher domainEventDispatcher,
    IModuleEventBus moduleEventBus) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // 1. Harvest domain events from entities
        var entities = dbContext.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // 2. Dispatch domain events to handlers (handlers may call eventBus.PublishAsync)
        foreach (var domainEvent in domainEvents)
        {
            await domainEventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        // Clear domain events after dispatching
        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        // 3. Capture buffered integration events from ModuleEventBus
        if (moduleEventBus is ModuleEventBus bus && dbContext is IOutboxSource outboxSource)
        {
            var integrationEvents = bus.GetBufferedEvents();

            foreach (var @event in integrationEvents)
            {
                var eventType = @event.GetType().FullName ?? @event.GetType().Name;
                var payload = DefaultSerializer.Serialize(@event);

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid7.NewGuid().ToGuid(),
                    EventType = eventType,
                    Payload = payload,
                    CreatedAt = SystemClock.Instance.GetCurrentInstant(),
                    ProcessedAt = null
                };

                outboxSource.OutboxMessages.Add(outboxMessage);
            }

            bus.ClearBufferedEvents();
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
