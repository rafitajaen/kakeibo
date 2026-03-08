using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

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
        [FromHeader(Name = "X-User-Id")] Guid userId,
        ListFriendsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, ct);
        return TypedResults.Ok(result);
    }
}
