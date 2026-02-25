using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.ChangePassword;

// Verifies the current password and replaces it with a new hash.
public sealed class ChangePasswordHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<ChangePasswordEndpoint.ChangePasswordResponse>> HandleAsync(
        ChangePasswordEndpoint.ChangePasswordRequest request,
        Guid userId,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Error.NotFound("User not found.");

        // Verify current password before allowing the change
        if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Error.Unauthorized("Current password is incorrect.");

        // Reject if new password is the same as the current one
        if (PasswordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
            return Error.Validation("New password must be different from the current password.");

        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return new ChangePasswordEndpoint.ChangePasswordResponse("Password changed successfully.");
    }
}
