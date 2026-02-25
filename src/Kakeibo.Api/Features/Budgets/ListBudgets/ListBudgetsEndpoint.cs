using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Budgets.ListBudgets;

public sealed class ListBudgetsEndpoint : IEndpoint
{
    public sealed record ListBudgetsResponse(
        Guid Id,
        string Name,
        Guid CategoryId,
        string CategoryName,
        decimal Limit,
        string StartDate,
        string EndDate,
        Guid? WalletId,
        string? WalletName,
        decimal CurrentSpending,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/budgets", HandleAsync)
            .WithTags("Budgets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        ListBudgetsHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        Guid? categoryId,
        Guid? walletId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, categoryId, walletId, ct);
        return TypedResults.Ok(result);
    }
}
