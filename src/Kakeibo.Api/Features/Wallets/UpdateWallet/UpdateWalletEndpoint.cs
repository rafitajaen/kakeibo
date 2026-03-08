using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.UpdateWallet;

public sealed class UpdateWalletEndpoint : IEndpoint
{
    public sealed record UpdateWalletRequest(
        string Name,
        string? Icon = null,
        string? BackgroundColor = null,
        string? TextColor = null);

    public sealed record UpdateWalletResponse(
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
        app.MapPut("/api/wallets/{id:guid}", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithValidation<UpdateWalletRequest>();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateWalletRequest request,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        UpdateWalletHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
