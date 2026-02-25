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

// An activity entry returned from the activity feed query.
public sealed record ActivityEntry(
    Guid Id,
    string Action,
    string? EntityType,
    Guid? EntityId,
    Instant OccurredAt);

// Appends audit events to the ClickHouse audit trail and provides activity feed queries.
public interface IAuditService
{
    Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ActivityEntry> Items, int Total)> GetActivitiesAsync(
        Guid userId,
        LocalDate? start,
        LocalDate? end,
        string? actionType,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);
}
