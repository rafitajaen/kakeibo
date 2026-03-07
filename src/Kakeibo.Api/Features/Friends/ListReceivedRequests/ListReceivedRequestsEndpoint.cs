using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

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
        ClaimsPrincipal principal,
        ListReceivedRequestsHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, ct);
        return TypedResults.Ok(result);
    }
}
