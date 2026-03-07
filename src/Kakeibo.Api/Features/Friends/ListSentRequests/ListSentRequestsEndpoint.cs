using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Friends.ListSentRequests;

public sealed class ListSentRequestsEndpoint : IEndpoint
{
    public sealed record ListSentRequestsResponse(
        Guid Id,
        Guid ReceiverUserId,
        string ReceiverUsername,
        string? ReceiverName,
        string? ReceiverAvatarUrl,
        string SentAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/friends/requests/sent", HandleAsync)
            .WithTags("Friends")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal principal,
        ListSentRequestsHandler handler,
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
