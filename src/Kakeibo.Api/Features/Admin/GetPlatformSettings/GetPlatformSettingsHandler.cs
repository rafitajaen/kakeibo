using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Admin.GetPlatformSettings;

// Returns the current platform-wide policy settings.
public sealed class GetPlatformSettingsHandler(AppDbContext db)
{
    public async Task<Result<GetPlatformSettingsEndpoint.GetPlatformSettingsResponse>> HandleAsync(
        CancellationToken ct)
    {
        var policy = await db.PlatformPolicy
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (policy is null)
        {
            return Error.NotFound("Platform policy not found.");
        }

        return new GetPlatformSettingsEndpoint.GetPlatformSettingsResponse(
            policy.RegistrationEnabled,
            policy.MaintenanceMode);
    }
}
