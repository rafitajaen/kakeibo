using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Transactions.Categories.ListCategories;

public sealed class ListCategoriesEndpoint : IEndpoint
{
    public sealed record ListCategoriesResponse(
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
        app.MapGet("/api/categories", HandleAsync)
            .WithTags("Categories")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        bool? includeArchived,
        Guid? walletId,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        ListCategoriesHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, includeArchived ?? false, walletId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
