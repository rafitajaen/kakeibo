using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Budgets.UpdateBudget;

public sealed class UpdateBudgetEndpoint : IEndpoint
{
    public sealed record UpdateBudgetRequest(
        string Name,
        Guid CategoryId,
        decimal Limit,
        string StartDate,
        string EndDate,
        Guid? WalletId);

    public sealed record UpdateBudgetResponse(
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
        app.MapPut("/api/budgets/{id:guid}", HandleAsync)
            .WithTags("Budgets")
            .RequireAuthorization()
            .WithValidation<UpdateBudgetRequest>();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateBudgetRequest request,
        UpdateBudgetHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
