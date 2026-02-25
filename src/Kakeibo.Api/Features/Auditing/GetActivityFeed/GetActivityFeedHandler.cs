using Kakeibo.Api.Infrastructure.Audit;
using NodaTime;

namespace Kakeibo.Api.Features.Auditing.GetActivityFeed;

// Queries the ClickHouse audit trail for the user's activity feed with optional filters.
public sealed class GetActivityFeedHandler(IAuditService auditService)
{
    public async Task<GetActivityFeedEndpoint.GetActivityFeedResponse> HandleAsync(
        Guid userId,
        LocalDate? start,
        LocalDate? end,
        string? actionType,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var maxPageSize = 100;
        var clampedSize = Math.Min(pageSize, maxPageSize);
        var clampedPage = Math.Max(page, 1);
        var offset = (clampedPage - 1) * clampedSize;

        var (items, total) = await auditService.GetActivitiesAsync(
            userId, start, end, actionType, offset, clampedSize, ct);

        var activityItems = items
            .Select(a => new GetActivityFeedEndpoint.ActivityItem(
                a.Id,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.OccurredAt.ToString()))
            .ToList();

        return new GetActivityFeedEndpoint.GetActivityFeedResponse(
            activityItems,
            total,
            clampedPage,
            clampedSize);
    }
}
