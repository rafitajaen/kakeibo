using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Identity.DeleteSeedData;

public sealed class DeleteSeedDataEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/users/me/seed-data", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        [FromHeader(Name = "X-User-Id")] Guid userId,
        DeleteSeedDataHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(userId, ct);
        return TypedResults.NoContent();
    }
}
