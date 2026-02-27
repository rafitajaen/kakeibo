using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.CreateWallet;

public sealed class CreateWalletEndpoint : IEndpoint
{
    public sealed record CreateWalletRequest(string Name, string Type);

    public sealed record CreateWalletResponse(
        Guid Id,
        string Name,
        string Type,
        string Currency,
        decimal Balance,
        bool IsArchived,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/wallets", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithValidation<CreateWalletRequest>();
    }

    private static async Task<IResult> HandleAsync(
        CreateWalletRequest request,
        ClaimsPrincipal principal,
        CreateWalletHandler handler,
        CancellationToken ct)
    {
        // Extract userId from JWT claims and pass to handler — keeps handler testable without HttpContext
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/wallets/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
