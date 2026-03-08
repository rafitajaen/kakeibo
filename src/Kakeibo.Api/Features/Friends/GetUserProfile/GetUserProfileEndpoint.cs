using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Friends.GetUserProfile;

public sealed class GetUserProfileEndpoint : IEndpoint
{
    public sealed record GetUserProfileResponse(
        Guid Id,
        string Username,
        string? Name,
        string? AvatarUrl,
        bool IsFriend,
        string? FriendsSince);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:guid}/profile", HandleAsync)
            .WithTags("Friends")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        GetUserProfileHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
