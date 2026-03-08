using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Friends.DeleteFriendship;

public sealed class DeleteFriendshipEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/friends/{id:guid}", HandleAsync)
            .WithTags("Friends")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        DeleteFriendshipHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, userId, ct);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
