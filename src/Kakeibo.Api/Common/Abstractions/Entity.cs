using NodaTime;

namespace Kakeibo.Api.Common.Abstractions;

// Base class for all entities with Guid7 ID, timestamps, and soft delete
public abstract class Entity
{
    public Guid Id { get; init; } = Utils.Guid7.NewGuid().ToGuid();
    public Instant CreatedAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public Instant UpdatedAt { get; set; } = SystemClock.Instance.GetCurrentInstant();
    public Instant? DeletedAt { get; set; }

    // Derived from DeletedAt — no redundant bool flag (see TD-016)
    public bool IsDeleted => DeletedAt is not null;
}
