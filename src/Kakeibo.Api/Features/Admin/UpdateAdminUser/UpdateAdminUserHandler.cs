using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Admin.UpdateAdminUser;

// Updates a user's name and/or role. Admin-only operation.
public sealed class UpdateAdminUserHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdateAdminUserEndpoint.UpdateAdminUserResponse>> HandleAsync(
        Guid userId,
        UpdateAdminUserEndpoint.UpdateAdminUserRequest request,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound("User not found.");
        }

        if (request.Name is not null)
        {
            user.Name = request.Name;
        }

        if (request.Role is not null &&
            Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            user.Role = role;
        }

        user.UpdatedAt = clock.GetCurrentInstant();
        await db.SaveChangesAsync(ct);

        return new UpdateAdminUserEndpoint.UpdateAdminUserResponse(
            user.Id, user.Email, user.Username, user.Name, user.Role.ToString());
    }
}
