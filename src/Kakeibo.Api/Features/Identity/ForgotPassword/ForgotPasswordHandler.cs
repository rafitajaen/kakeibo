using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.ForgotPassword;

// Generates a password reset token and sends a reset link to the user's email.
public sealed class ForgotPasswordHandler(AppDbContext db, IEmailService emailService, IClock clock)
{
    private static readonly Duration ResetTokenExpiry = Duration.FromHours(1);

    public async Task<Result<ForgotPasswordEndpoint.ForgotPasswordResponse>> HandleAsync(
        ForgotPasswordEndpoint.ForgotPasswordRequest request,
        CancellationToken ct)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        // Return success regardless — prevents email enumeration
        if (user is null)
            return new ForgotPasswordEndpoint.ForgotPasswordResponse(
                "If that email exists, a reset link has been sent.");

        var now = clock.GetCurrentInstant();

        var rawToken = RandomString.Generate(32, CharSets.Alphanumeric);
        var tokenHash = TokenHasher.Hash(rawToken);

        var resetToken = new PasswordResetToken
        {
            TokenHash = tokenHash,
            UserId = user.Id,
            ExpiresAt = now.Plus(ResetTokenExpiry)
        };

        db.PasswordResetTokens.Add(resetToken);
        await db.SaveChangesAsync(ct);

        // Send reset email asynchronously
        _ = emailService.SendPasswordResetEmailAsync(
            user.Id, user.Email, user.Email, rawToken, CancellationToken.None);

        return new ForgotPasswordEndpoint.ForgotPasswordResponse(
            "If that email exists, a reset link has been sent.");
    }
}
