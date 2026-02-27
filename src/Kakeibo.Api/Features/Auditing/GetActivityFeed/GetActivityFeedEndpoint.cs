using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Auditing.GetActivityFeed;

public sealed class GetActivityFeedEndpoint : IEndpoint
{
    public sealed record ActivityItem(
        Guid Id,
        string Action,
        string? EntityType,
        Guid? EntityId,
        string OccurredAt);

    public sealed record GetActivityFeedResponse(
        IReadOnlyList<ActivityItem> Items,
        int Total,
        int Page,
        int PageSize);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/activity", HandleAsync)
            .WithTags("Auditing")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        GetActivityFeedHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromQuery] string? start,
        [FromQuery] string? end,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        LocalDate? startDate = null;
        LocalDate? endDate = null;

        // Parse ISO date strings (yyyy-MM-dd) into LocalDate using NodaTime pattern
        var datePattern = LocalDatePattern.Iso;
        if (!string.IsNullOrEmpty(start))
        {
            var parseResult = datePattern.Parse(start);
            if (parseResult.Success)
            {
                startDate = parseResult.Value;
            }
        }
        if (!string.IsNullOrEmpty(end))
        {
            var parseResult = datePattern.Parse(end);
            if (parseResult.Success)
            {
                endDate = parseResult.Value;
            }
        }

        var result = await handler.HandleAsync(userId, startDate, endDate, type, page, pageSize, ct);
        return TypedResults.Ok(result);
    }
}
