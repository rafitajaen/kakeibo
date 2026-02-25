using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Budgets.CreateBudget;

public sealed class CreateBudgetEndpoint : IEndpoint
{
    public sealed record CreateBudgetRequest(
        string Name,
        Guid CategoryId,
        decimal Limit,
        string StartDate,
        string EndDate,
        Guid? WalletId);

    public sealed record CreateBudgetResponse(
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
        app.MapPost("/api/budgets", HandleAsync)
            .WithTags("Budgets")
            .RequireAuthorization()
            .WithValidation<CreateBudgetRequest>();
    }

    private static async Task<IResult> HandleAsync(
        CreateBudgetRequest request,
        CreateBudgetHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/budgets/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
