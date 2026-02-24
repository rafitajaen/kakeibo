using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.ResetPassword;

// Resets a user's password using a valid, unused, non-expired reset token.
public sealed class ResetPasswordHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<ResetPasswordEndpoint.ResetPasswordResponse>> HandleAsync(
        ResetPasswordEndpoint.ResetPasswordRequest request,
        CancellationToken ct)
    {
        var tokenHash = TokenHasher.Hash(request.Token);
        var now = clock.GetCurrentInstant();

        var resetToken = await db.PasswordResetTokens
            .Include(prt => prt.User)
            .FirstOrDefaultAsync(prt =>
                prt.TokenHash == tokenHash &&
                prt.ExpiresAt > now &&
                prt.UsedAt == null, ct);

        if (resetToken is null || resetToken.User is null)
            return Error.NotFound("Invalid or expired password reset token.");

        // Mark token as used — single-use only
        resetToken.UsedAt = now;
        resetToken.UpdatedAt = now;

        // Update the user's password
        resetToken.User.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        resetToken.User.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return new ResetPasswordEndpoint.ResetPasswordResponse("Password reset successfully.");
    }
}
