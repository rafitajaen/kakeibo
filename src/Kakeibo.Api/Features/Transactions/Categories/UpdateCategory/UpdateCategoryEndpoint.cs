using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Transactions.Categories.UpdateCategory;

public sealed class UpdateCategoryEndpoint : IEndpoint
{
    public sealed record UpdateCategoryRequest(string Name);

    public sealed record UpdateCategoryResponse(
        Guid Id,
        string Name,
        bool IsSystem,
        bool IsArchived);

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
        ClaimsPrincipal principal,
        UpdateCategoryHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(id, request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.StatusCode(403),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
