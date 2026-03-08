using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Identity.SeedTestData;

public sealed class SeedTestDataEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/me/seed-data", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        [FromHeader(Name = "X-User-Id")] Guid userId,
        SeedTestDataHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, ct);
        return result.IsSuccess
            ? result.Value
                ? TypedResults.Created("/api/users/me/seed-data", new { message = "Seed data created." })
                : TypedResults.NoContent()
            : TypedResults.Problem(result.Error.Message, statusCode: 500);
    }
}
