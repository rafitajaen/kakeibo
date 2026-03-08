using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
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
        Guid? DestinationWalletId,
        string? Notes = null);

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
        string? Notes,
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
        [FromHeader(Name = "X-User-Id")] Guid userId,
        RecordTransactionHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/transactions/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "validation" => TypedResults.BadRequest(result.Error),
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
