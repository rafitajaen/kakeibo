using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Transactions.Categories.CreateCategory;

public sealed class CreateCategoryEndpoint : IEndpoint
{
    public sealed record CreateCategoryRequest(string Name);

    public sealed record CreateCategoryResponse(
        Guid Id,
        string Name,
        bool IsSystem,
        bool IsArchived);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/categories", HandleAsync)
            .WithTags("Categories")
            .RequireAuthorization()
            .WithValidation<CreateCategoryRequest>();
    }

    private static async Task<IResult> HandleAsync(
        CreateCategoryRequest request,
        ClaimsPrincipal principal,
        CreateCategoryHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

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
