using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kakeibo.Api.Features.Admin.GetPlatformSettings;

public sealed class GetPlatformSettingsEndpoint : IEndpoint
{
    public sealed record GetPlatformSettingsResponse(
        bool RegistrationEnabled,
        bool MaintenanceMode);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/settings", HandleAsync)
            .WithTags("Admin")
            .RequireAuthorization("Admin");
    }

    private static async Task<Results<Ok<GetPlatformSettingsResponse>, ForbidHttpResult>> HandleAsync(
        GetPlatformSettingsHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Forbid();
    }
}
