using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NodaTime;
using System.Security.Claims;

namespace Kakeibo.Api.Features.Wallets.GetWallet;

public sealed class GetWalletEndpoint : IEndpoint
{
    public sealed record GetWalletResponse(
        Guid Id,
        string Name,
        string Type,
        string Currency,
        decimal Balance,
        bool IsArchived,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wallets/{id:guid}", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ClaimsPrincipal principal,
        GetWalletHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return TypedResults.Unauthorized();

        var result = await handler.HandleAsync(id, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.StatusCode(403),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
