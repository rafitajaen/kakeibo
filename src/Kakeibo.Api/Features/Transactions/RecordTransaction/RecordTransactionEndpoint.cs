using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.RecordTransaction;

public sealed class RecordTransactionEndpoint : IEndpoint
{
    public sealed record RecordTransactionRequest(
        string Type,
        decimal Amount,
        string Description,
        string Date,
        Guid CategoryId,
        Guid WalletId,
        Guid? DestinationWalletId);

    public sealed record RecordTransactionResponse(
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
        app.MapPost("/api/transactions", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization()
            .WithValidation<RecordTransactionRequest>();
    }

    private static async Task<IResult> HandleAsync(
        RecordTransactionRequest request,
        ClaimsPrincipal principal,
        RecordTransactionHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/transactions/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "validation" => TypedResults.BadRequest(result.Error),
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.StatusCode(403),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
