using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.GetTransaction;

public sealed class GetTransactionEndpoint : IEndpoint
{
    public sealed record GetTransactionResponse(
        Guid Id,
        string Type,
        decimal Amount,
        string? Description,
        string Date,
        Guid CategoryId,
        string CategoryName,
        Guid WalletId,
        Guid? DestinationWalletId,
        Guid UserId,
        string? Notes,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions/{id:guid}", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        GetTransactionHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, userId, ct);
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
