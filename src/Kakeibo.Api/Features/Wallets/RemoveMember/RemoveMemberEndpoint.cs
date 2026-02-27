using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Wallets.RemoveMember;

public sealed class RemoveMemberEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/wallets/{id:guid}/members/{userId:guid}", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid userId,
        ClaimsPrincipal principal,
        RemoveMemberHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var requesterId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(id, userId, requesterId, ct);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.StatusCode(403),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
