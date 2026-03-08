using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

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
        [FromHeader(Name = "X-User-Id")] Guid requesterId,
        RemoveMemberHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, userId, requesterId, ct);
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
