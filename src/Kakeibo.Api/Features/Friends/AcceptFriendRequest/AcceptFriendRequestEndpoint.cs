using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Friends.AcceptFriendRequest;

public sealed class AcceptFriendRequestEndpoint : IEndpoint
{
    public sealed record AcceptFriendRequestResponse(Guid FriendshipId);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/friends/requests/{id:guid}/accept", HandleAsync)
            .WithTags("Friends")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        AcceptFriendRequestHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
