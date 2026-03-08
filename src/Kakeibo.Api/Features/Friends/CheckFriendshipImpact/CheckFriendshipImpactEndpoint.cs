using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Friends.CheckFriendshipImpact;

public sealed class CheckFriendshipImpactEndpoint : IEndpoint
{
    public sealed record AffectedWalletResponse(
        Guid Id,
        string Name,
        string YourRole,
        bool WillLoseAccess);

    public sealed record CheckFriendshipImpactResponse(
        List<AffectedWalletResponse> SharedWallets,
        int GuestAccessRevoked);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/friends/{id:guid}/impact", HandleAsync)
            .WithTags("Friends")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CheckFriendshipImpactHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
