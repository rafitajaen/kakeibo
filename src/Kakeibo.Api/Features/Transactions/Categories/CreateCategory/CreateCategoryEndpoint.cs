using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Transactions.Categories.CreateCategory;

public sealed class CreateCategoryEndpoint : IEndpoint
{
    public sealed record CreateCategoryRequest(
        string Name,
        string? BackgroundColor = null,
        string? TextColor = null,
        string? Icon = null,
        bool IsPrivate = false);

    public sealed record CreateCategoryResponse(
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
        app.MapPost("/api/categories", HandleAsync)
            .WithTags("Categories")
            .RequireAuthorization()
            .WithValidation<CreateCategoryRequest>();
    }

    private static async Task<IResult> HandleAsync(
        CreateCategoryRequest request,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CreateCategoryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/categories/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
