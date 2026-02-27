using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.UpdateTransaction;

public sealed class UpdateTransactionEndpoint : IEndpoint
{
    public sealed record UpdateTransactionRequest(
        decimal Amount,
        string Description,
        string Date,
        Guid CategoryId,
        Guid? DestinationWalletId);

    public sealed record UpdateTransactionResponse(
        Guid Id,
        string Type,
        decimal Amount,
        string Description,
        string Date,
        Guid CategoryId,
        Guid WalletId,
        Guid? DestinationWalletId,
        Guid UserId,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/transactions/{id:guid}", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization()
            .WithValidation<UpdateTransactionRequest>();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateTransactionRequest request,
        ClaimsPrincipal principal,
        UpdateTransactionHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(id, request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "validation" => TypedResults.BadRequest(result.Error),
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.StatusCode(403),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
