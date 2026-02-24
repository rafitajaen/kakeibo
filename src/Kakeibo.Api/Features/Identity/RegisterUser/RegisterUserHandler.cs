using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.RegisterUser;

// Registers a new user account and sends an email verification link.
public sealed class RegisterUserHandler(
    AppDbContext db,
    IEventBus eventBus,
    IEmailService emailService,
    IClock clock)
{
    private static readonly Duration VerificationTokenExpiry = Duration.FromHours(24);

    public async Task<Result<RegisterUserEndpoint.RegisterUserResponse>> HandleAsync(
        RegisterUserEndpoint.RegisterUserRequest request,
        CancellationToken ct)
    {
        // Check for existing account with same email
        var exists = await db.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (exists)
            return Error.Conflict($"An account with email '{request.Email}' already exists.");

        var now = clock.GetCurrentInstant();

        // Generate email verification token
        var rawToken = RandomString.Generate(32, CharSets.Alphanumeric);
        var tokenHash = TokenHasher.Hash(rawToken);

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Currency = request.Currency,
            EmailVerificationToken = tokenHash,
            EmailVerificationTokenExpiresAt = now.Plus(VerificationTokenExpiry)
        };

        db.Users.Add(user);

        // Publish event before saving so it's part of the same logical unit
        eventBus.Publish(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email
        });

        await db.SaveChangesAsync(ct);

        // Send verification email asynchronously (non-blocking for the response)
        _ = emailService.SendWelcomeEmailAsync(user.Id, user.Email, user.Email, CancellationToken.None);

        return new RegisterUserEndpoint.RegisterUserResponse(user.Id, user.Email);
    }
}
