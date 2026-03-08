using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Admin.UpdatePlatformSettings;

// Updates platform-wide policy settings. Only callable by admins.
public sealed class UpdatePlatformSettingsHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdatePlatformSettingsEndpoint.UpdatePlatformSettingsResponse>> HandleAsync(
        UpdatePlatformSettingsEndpoint.UpdatePlatformSettingsRequest request,
        CancellationToken ct)
    {
        var policy = await db.PlatformPolicy.FirstOrDefaultAsync(ct);

        if (policy is null)
        {
            return Error.NotFound("Platform policy not found.");
        }

        policy.RegistrationEnabled = request.RegistrationEnabled;
        policy.MaintenanceMode = request.MaintenanceMode;
        policy.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return new UpdatePlatformSettingsEndpoint.UpdatePlatformSettingsResponse(
            policy.RegistrationEnabled,
            policy.MaintenanceMode);
    }
}
