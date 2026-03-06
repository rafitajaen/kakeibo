using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.ExportData;

public sealed class ExportDataEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/me/export", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        string format,
        ClaimsPrincipal principal,
        ExportDataHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return TypedResults.Unauthorized();

        var normalizedFormat = format.ToLowerInvariant();

        if (normalizedFormat != "sqlite" && normalizedFormat != "csv")
            return TypedResults.BadRequest("Format must be 'sqlite' or 'csv'.");

        var result = await handler.HandleAsync(userId, normalizedFormat, ct);

        return result.IsSuccess
            ? result.Value
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                _           => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
