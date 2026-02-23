using NodaTime;

namespace Kakeibo.Common.Abstractions;

// Base class for all entities with Guid7 ID, timestamps, soft delete, and domain events
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; init; } = Utils.Guid7.NewGuid().ToGuid();
    public Instant CreatedAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public Instant UpdatedAt { get; set; } = SystemClock.Instance.GetCurrentInstant();
    public bool IsDeleted { get; set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
