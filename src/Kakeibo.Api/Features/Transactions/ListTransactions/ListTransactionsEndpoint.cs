using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.ListTransactions;

public sealed class ListTransactionsEndpoint : IEndpoint
{
    public sealed record TransactionItem(
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

    public sealed record ListTransactionsResponse(
        IReadOnlyList<TransactionItem> Items,
        int Total);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid walletId,
        int? page,
        int? pageSize,
        string? from,
        string? to,
        Guid? categoryId,
        string? type,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        ListTransactionsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            walletId, userId,
            page ?? 1, pageSize ?? 50,
            from, to, categoryId, type,
            ct);

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
