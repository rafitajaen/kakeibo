using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Goals.GetGoalProgress;

// Returns real-time progress details for a savings goal with computed fields.
public sealed class GetGoalProgressHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<GetGoalProgressEndpoint.GetGoalProgressResponse>> HandleAsync(
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

        // Compute derived fields from current progress and target amount
        var remaining = Math.Max(0m, goal.TargetAmount - goal.CurrentProgress);
        var percentageComplete = goal.TargetAmount > 0
            ? (goal.CurrentProgress / goal.TargetAmount) * 100m
            : 0m;

        // Determine status: NotStarted → InProgress → NearTarget (≥75%) → Achieved (≥100%)
        var status = goal.CurrentProgress >= goal.TargetAmount
            ? "Achieved"
            : percentageComplete >= 75m
                ? "NearTarget"
                : goal.CurrentProgress > 0
                    ? "InProgress"
                    : "NotStarted";

        // Calculate days remaining from today to deadline; null if no deadline set
        int? daysRemaining = null;
        if (goal.Deadline.HasValue)
        {
            var today = clock.GetCurrentInstant().InUtc().Date;
            daysRemaining = Period.Between(today, goal.Deadline.Value, PeriodUnits.Days).Days;
        }

        return new GetGoalProgressEndpoint.GetGoalProgressResponse(
            goal.Id,
            goal.Name,
            goal.TargetAmount,
            goal.CurrentProgress,
            remaining,
            percentageComplete,
            status,
            goal.Deadline.HasValue ? LocalDatePattern.Iso.Format(goal.Deadline.Value) : null,
            daysRemaining);
    }
}
