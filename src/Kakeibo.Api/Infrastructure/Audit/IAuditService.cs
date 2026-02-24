using NodaTime;

namespace Kakeibo.Api.Infrastructure.Audit;

// Entry representing a single auditable action
public sealed record AuditEntry(
    Guid UserId,
    string Action,
    string? EntityType,
    Guid? EntityId,
    Instant OccurredAt,
    string? IpAddress,
    string? UserAgent,
    string? Changes);

// Appends audit events to the ClickHouse audit trail.
public interface IAuditService
{
    Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
