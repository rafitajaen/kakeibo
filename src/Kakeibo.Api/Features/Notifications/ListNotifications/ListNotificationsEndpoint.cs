using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Notifications.ListNotifications;

public sealed class ListNotificationsEndpoint : IEndpoint
{
    public sealed record NotificationItem(
        Guid Id,
        string Type,
        string Title,
        string Body,
        bool IsRead,
        string CreatedAt);

    public sealed record ListNotificationsResponse(
        int UnreadCount,
        IReadOnlyList<NotificationItem> Items);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications", HandleAsync)
            .WithTags("Notifications")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        ListNotificationsHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await handler.HandleAsync(userId, page, pageSize, ct);
        return TypedResults.Ok(result);
    }
}
