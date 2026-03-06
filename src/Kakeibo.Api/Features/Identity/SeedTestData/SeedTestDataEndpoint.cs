using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

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
        ClaimsPrincipal principal,
        SeedTestDataHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, ct);
        return result.IsSuccess
            ? result.Value
                ? TypedResults.Created("/api/users/me/seed-data", new { message = "Seed data created." })
                : TypedResults.NoContent()
            : TypedResults.Problem(result.Error.Message, statusCode: 500);
    }
}
