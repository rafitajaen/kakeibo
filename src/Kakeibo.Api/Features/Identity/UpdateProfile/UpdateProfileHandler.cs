using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.UpdateProfile;

// Updates the display name and/or currency of the authenticated user.
public sealed class UpdateProfileHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdateProfileEndpoint.UpdateProfileResponse>> HandleAsync(
        UpdateProfileEndpoint.UpdateProfileRequest request,
        Guid userId,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Error.NotFound("User not found.");

        var now = clock.GetCurrentInstant();

        // Apply only the fields that were provided in the request
        if (request.Name is not null)
            user.Name = request.Name.Trim().Length == 0 ? null : request.Name.Trim();

        if (request.Currency is not null)
            user.Currency = request.Currency.ToUpperInvariant();

        user.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return new UpdateProfileEndpoint.UpdateProfileResponse(
            user.Id, user.Email, user.Name, user.Currency);
    }
}
