using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kakeibo.Api.Features.Wallets.TransferWallet;

public sealed class TransferWalletEndpoint : IEndpoint
{
    public sealed record TransferWalletRequest(Guid ToUserId);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/wallets/{id:guid}/transfer", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithValidation<TransferWalletRequest>();
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, BadRequest<string>>> HandleAsync(
        Guid id,
        TransferWalletRequest request,
        [Microsoft.AspNetCore.Mvc.FromHeader(Name = "X-User-Id")] Guid userId,
        TransferWalletHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, userId, request.ToUserId, ct);
        if (!result.IsSuccess)
        {
            return result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(),
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.BadRequest(result.Error.Message)
            };
        }

        return TypedResults.NoContent();
    }
}
