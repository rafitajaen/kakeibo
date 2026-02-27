using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.DeleteAccount;

// Marks the user account for deletion after a 30-day GDPR grace period.
// Revokes all active sessions immediately. A background job performs permanent deletion after the grace period.
public sealed class DeleteAccountHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<DeleteAccountEndpoint.DeleteAccountResponse>> HandleAsync(
        DeleteAccountEndpoint.DeleteAccountRequest request,
        Guid userId,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound("User not found.");
        }

        // Require password confirmation to prevent accidental deletions
        if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Error.Unauthorized("Password is incorrect.");
        }

        var now = clock.GetCurrentInstant();

        // Mark for deletion — permanent deletion handled by AccountDeletionJob after 30 days
        user.DeletionRequestedAt = now;
        user.UpdatedAt = now;

        // Revoke all active refresh tokens to end all sessions immediately
        var activeTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);

        // Clear current session cookies
        httpContext.Response.Cookies.Delete("access_token");
        httpContext.Response.Cookies.Delete("refresh_token");

        return new DeleteAccountEndpoint.DeleteAccountResponse(
            "Account deletion requested. Your data will be permanently deleted within 30 days. Log in before then to cancel.");
    }
}
