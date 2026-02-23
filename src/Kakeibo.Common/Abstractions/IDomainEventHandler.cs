namespace Kakeibo.Common.Abstractions;

// Handler for domain events - dispatched by OutboxInterceptor during SaveChangesAsync
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
