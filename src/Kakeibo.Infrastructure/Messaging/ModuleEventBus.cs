using Kakeibo.Common.Abstractions;
using Kakeibo.Common.Modules;

namespace Kakeibo.Infrastructure.Messaging;

// Buffers integration events in-memory (scoped lifetime) for capture by OutboxInterceptor.
// Events are captured during SaveChangesAsync and persisted in outbox within the same transaction.
public sealed class ModuleEventBus : IModuleEventBus
{
    private readonly List<IIntegrationEvent> _bufferedEvents = [];

    public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _bufferedEvents.Add(@event);
        return Task.CompletedTask;
    }

    public IReadOnlyList<IIntegrationEvent> GetBufferedEvents() => _bufferedEvents.AsReadOnly();

    public void ClearBufferedEvents() => _bufferedEvents.Clear();
}
