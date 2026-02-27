using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.VerifyEmail;

// Verifies an email address using the token sent during registration.
public sealed class VerifyEmailHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<VerifyEmailEndpoint.VerifyEmailResponse>> HandleAsync(
        VerifyEmailEndpoint.VerifyEmailRequest request,
        CancellationToken ct)
    {
        var tokenHash = TokenHasher.Hash(request.Token);
        var now = clock.GetCurrentInstant();

        var user = await db.Users
            .FirstOrDefaultAsync(u =>
                u.EmailVerificationToken == tokenHash &&
                u.EmailVerificationTokenExpiresAt > now &&
                !u.IsVerified, ct);

        if (user is null)
        {
            return Error.NotFound("Invalid or expired verification token.");
        }

        user.IsVerified = true;
        user.VerifiedAt = now;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;
        user.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return new VerifyEmailEndpoint.VerifyEmailResponse("Email verified successfully.");
    }
}
