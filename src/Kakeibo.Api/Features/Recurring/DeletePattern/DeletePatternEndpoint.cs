using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Recurring.DeletePattern;

public sealed class DeletePatternEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/recurring-patterns/{id:guid}", HandleAsync)
            .WithTags("Recurring")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        DeletePatternHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, userId, ct);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                _           => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
