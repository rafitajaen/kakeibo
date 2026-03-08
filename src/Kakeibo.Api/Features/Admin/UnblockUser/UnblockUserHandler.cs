using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Admin.UnblockUser;

// Removes the block from a user account, restoring their ability to authenticate.
public sealed class UnblockUserHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound("User not found.");
        }

        if (user.IsBlocked)
        {
            var now = clock.GetCurrentInstant();
            user.IsBlocked = false;
            user.BlockedAt = null;
            user.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
        }

        return true;
    }
}
