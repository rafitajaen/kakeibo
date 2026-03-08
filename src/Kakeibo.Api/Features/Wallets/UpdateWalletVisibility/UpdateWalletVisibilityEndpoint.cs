using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Wallets.UpdateWalletVisibility;

public sealed class UpdateWalletVisibilityEndpoint : IEndpoint
{
    public sealed record UpdateWalletVisibilityRequest(string Visibility);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/wallets/{id:guid}/visibility", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateWalletVisibilityRequest request,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        UpdateWalletVisibilityHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, request, userId, ct);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
