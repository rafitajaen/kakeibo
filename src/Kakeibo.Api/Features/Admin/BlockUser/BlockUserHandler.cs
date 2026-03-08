using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Admin.BlockUser;

// Blocks a user account. Blocked users cannot authenticate. Admins cannot be blocked.
public sealed class BlockUserHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound("User not found.");
        }

        if (user.Role == UserRole.Admin)
        {
            return Error.Validation("Admin accounts cannot be blocked.");
        }

        if (!user.IsBlocked)
        {
            var now = clock.GetCurrentInstant();
            user.IsBlocked = true;
            user.BlockedAt = now;
            user.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
        }

        return true;
    }
}
