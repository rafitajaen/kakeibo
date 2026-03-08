using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.ListPublicWallets;

public sealed class ListPublicWalletsEndpoint : IEndpoint
{
    public sealed record PublicWalletResponse(
        Guid Id,
        string Name,
        string Type,
        string Currency,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userId:guid}/wallets", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid userId,
        [FromHeader(Name = "X-User-Id")] Guid callerId,
        ListPublicWalletsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, callerId, ct);
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
