using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Transactions.Categories.ListCategories;

public sealed class ListCategoriesEndpoint : IEndpoint
{
    public sealed record ListCategoriesResponse(
        Guid Id,
        string Name,
        bool IsSystem,
        bool IsArchived,
        string? BackgroundColor,
        string? TextColor,
        string? Icon);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories", HandleAsync)
            .WithTags("Categories")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        bool? includeArchived,
        ClaimsPrincipal principal,
        ListCategoriesHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, includeArchived ?? false, ct);
        return TypedResults.Ok(result);
    }
}
