using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.ListAllTransactions;

public sealed class ListAllTransactionsEndpoint : IEndpoint
{
    public sealed record TransactionItem(
        Guid Id,
        string Type,
        decimal Amount,
        string? Description,
        string Date,
        Guid CategoryId,
        string CategoryName,
        string? CategoryColor,
        string? CategoryIcon,
        Guid WalletId,
        string WalletName,
        Guid? DestinationWalletId,
        string? Notes,
        Instant CreatedAt);

    public sealed record ListAllTransactionsResponse(
        IReadOnlyList<TransactionItem> Items,
        int Total);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions/all", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        int? page,
        int? pageSize,
        string? from,
        string? to,
        Guid? categoryId,
        string? type,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        ListAllTransactionsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            userId,
            page ?? 1, pageSize ?? 50,
            from, to, categoryId, type,
            ct);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(result.Error.Message, statusCode: 500);
    }
}
