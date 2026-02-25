using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Budgets.GetBudgetStatus;

public sealed class GetBudgetStatusEndpoint : IEndpoint
{
    public sealed record GetBudgetStatusResponse(
        Guid Id,
        string Name,
        Guid CategoryId,
        string CategoryName,
        decimal Limit,
        decimal CurrentSpending,
        decimal Remaining,
        decimal PercentageUsed,
        string Status,
        string StartDate,
        string EndDate,
        Guid? WalletId,
        string? WalletName);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/budgets/{id:guid}/status", HandleAsync)
            .WithTags("Budgets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetBudgetStatusHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
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
