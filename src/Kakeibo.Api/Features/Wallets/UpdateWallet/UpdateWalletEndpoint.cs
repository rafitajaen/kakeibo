using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NodaTime;
using System.Security.Claims;

namespace Kakeibo.Api.Features.Wallets.UpdateWallet;

public sealed class UpdateWalletEndpoint : IEndpoint
{
    public sealed record UpdateWalletRequest(string Name);

    public sealed record UpdateWalletResponse(
        Guid Id,
        string Name,
        string Type,
        string Currency,
        decimal Balance,
        bool IsArchived,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/wallets/{id:guid}", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithValidation<UpdateWalletRequest>();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateWalletRequest request,
        ClaimsPrincipal principal,
        UpdateWalletHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return TypedResults.Unauthorized();

        var result = await handler.HandleAsync(id, request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.StatusCode(403),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
