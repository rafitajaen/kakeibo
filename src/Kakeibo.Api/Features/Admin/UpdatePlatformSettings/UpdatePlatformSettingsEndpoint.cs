using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kakeibo.Api.Features.Admin.UpdatePlatformSettings;

public sealed class UpdatePlatformSettingsEndpoint : IEndpoint
{
    public sealed record UpdatePlatformSettingsRequest(
        bool RegistrationEnabled,
        bool MaintenanceMode);

    public sealed record UpdatePlatformSettingsResponse(
        bool RegistrationEnabled,
        bool MaintenanceMode);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/admin/settings", HandleAsync)
            .WithTags("Admin")
            .RequireAuthorization("Admin")
            .WithValidation<UpdatePlatformSettingsRequest>();
    }

    private static async Task<Results<Ok<UpdatePlatformSettingsResponse>, NotFound>> HandleAsync(
        UpdatePlatformSettingsRequest request,
        UpdatePlatformSettingsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.NotFound();
    }
}
