using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Goals.DeleteGoal;

// Soft-deletes a goal, preserving audit trail.
public sealed class DeleteGoalHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(
        Guid goalId,
        Guid userId,
        CancellationToken ct)
    {
        var goal = await db.Goals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.DeletedAt == null, ct);

        if (goal is null)
            return Error.NotFound("Goal not found.");

        if (goal.UserId != userId)
            return Error.Forbidden("You do not have access to this goal.");

        goal.DeletedAt = clock.GetCurrentInstant();
        goal.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return true;
    }
}
