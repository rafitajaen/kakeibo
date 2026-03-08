using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Wallets.RevokeInvitation;

public sealed class RevokeInvitationEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/wallets/{id:guid}/invitations/{code}", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        string code,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        RevokeInvitationHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, code, userId, ct);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
