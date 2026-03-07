using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Friends.ListFriends;

public sealed class ListFriendsEndpoint : IEndpoint
{
    public sealed record ListFriendsResponse(
        Guid FriendshipId,
        Guid UserId,
        string Username,
        string? Name,
        string? AvatarUrl,
        string FriendsSince);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/friends", HandleAsync)
            .WithTags("Friends")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal principal,
        ListFriendsHandler handler,
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
