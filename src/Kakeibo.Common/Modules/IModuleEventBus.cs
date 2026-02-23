using Kakeibo.Common.Abstractions;

namespace Kakeibo.Common.Modules;

// Publisher for async inter-module events (buffered, persisted in outbox)
public interface IModuleEventBus
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
