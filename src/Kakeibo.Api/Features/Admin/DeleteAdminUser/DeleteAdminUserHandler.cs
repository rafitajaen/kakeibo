using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Admin.DeleteAdminUser;

// Soft-deletes a user account. Admin accounts cannot be deleted this way.
public sealed class DeleteAdminUserHandler(AppDbContext db, IClock clock)
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
            return Error.Validation("Admin accounts cannot be deleted via this endpoint.");
        }

        // Schedule for deletion via the 30-day grace period (same as user self-deletion)
        var now = clock.GetCurrentInstant();
        user.DeletionRequestedAt = now;
        user.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        return true;
    }
}
