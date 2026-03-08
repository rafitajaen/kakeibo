using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Transactions.Categories.UpdateCategory;

public sealed class UpdateCategoryEndpoint : IEndpoint
{
    public sealed record UpdateCategoryRequest(
        string Name,
        string? BackgroundColor = null,
        string? TextColor = null,
        string? Icon = null,
        bool IsPrivate = false);

    public sealed record UpdateCategoryResponse(
        Guid Id,
        string Name,
        bool IsSystem,
        bool IsArchived,
        bool IsPrivate,
        string? BackgroundColor,
        string? TextColor,
        string? Icon);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/categories/{id:guid}", HandleAsync)
            .WithTags("Categories")
            .RequireAuthorization()
            .WithValidation<UpdateCategoryRequest>();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateCategoryRequest request,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        UpdateCategoryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
