using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Admin.CreateAdminUser;

// Creates a user account directly (bypasses registration flow and email verification).
public sealed class CreateAdminUserHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<CreateAdminUserEndpoint.CreateAdminUserResponse>> HandleAsync(
        CreateAdminUserEndpoint.CreateAdminUserRequest request,
        CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);
        if (exists)
        {
            return Error.Conflict($"An account with email '{request.Email}' already exists.");
        }

        // Parse role — default to User if unrecognised
        var role = Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var parsed)
            ? parsed
            : UserRole.User;

        var now = clock.GetCurrentInstant();

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Username = UsernameGenerator.Generate(),
            Role = role,
            Currency = request.Currency,
            Name = request.Name,
            IsVerified = true,
            VerifiedAt = now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new CreateAdminUserEndpoint.CreateAdminUserResponse(
            user.Id, user.Email, user.Username, user.Role.ToString());
    }
}
