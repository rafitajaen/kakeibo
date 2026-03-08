using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.CreateWallet;

public sealed class CreateWalletEndpoint : IEndpoint
{
    public sealed record CreateWalletRequest(
        string Name,
        string Type,
        decimal InitialBalance = 0m,
        string? Icon = null,
        string? BackgroundColor = null,
        string? TextColor = null);

    public sealed record CreateWalletResponse(
        Guid Id,
        string Name,
        string Type,
        string Currency,
        decimal Balance,
        bool IsArchived,
        string? Icon,
        string? BackgroundColor,
        string? TextColor,
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
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CreateWalletHandler handler,
        CancellationToken ct)
    {
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
