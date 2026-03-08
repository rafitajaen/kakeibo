using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Friends.ListReceivedRequests;

public sealed class ListReceivedRequestsEndpoint : IEndpoint
{
    public sealed record ListReceivedRequestsResponse(
        Guid Id,
        Guid SenderUserId,
        string SenderUsername,
        string? SenderName,
        string? SenderAvatarUrl,
        string SentAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/friends/requests", HandleAsync)
            .WithTags("Friends")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        [FromHeader(Name = "X-User-Id")] Guid userId,
        ListReceivedRequestsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, ct);
        return TypedResults.Ok(result);
    }
}
