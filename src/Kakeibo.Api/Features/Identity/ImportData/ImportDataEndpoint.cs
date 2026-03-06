using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.ImportData;

public sealed class ImportDataEndpoint : IEndpoint
{
    // Validated request model for multipart/form-data upload
    public sealed record ImportDataRequest(IFormFile? File, string Source);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/me/import", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithValidation<ImportDataRequest>();
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] ImportDataRequest request,
        ClaimsPrincipal principal,
        ImportDataHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return TypedResults.Unauthorized();

        var file = request.File!;
        await using var stream = file.OpenReadStream();

        var result = await handler.HandleAsync(stream, file.FileName, request.Source, userId, ct);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "validation" => TypedResults.UnprocessableEntity(result.Error),
                "not_found"  => TypedResults.NotFound(result.Error),
                _            => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
