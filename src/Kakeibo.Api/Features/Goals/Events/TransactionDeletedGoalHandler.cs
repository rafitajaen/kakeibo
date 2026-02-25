using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Goals.Events;

// Recalculates CurrentProgress for goals linked to the deleted transaction's wallet.
// Uses WalletBalance.Balance for self-healing correctness.
// Transaction deletion typically decreases balance and does not trigger new milestones,
// but the LastMilestone guard ensures no duplicate events regardless.
public sealed class TransactionDeletedGoalHandler(AppDbContext db, IEventBus eventBus)
    : IEventHandler<TransactionDeletedEvent>
{
    private static readonly int[] Milestones = [25, 50, 75, 100];

    public async Task HandleAsync(TransactionDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        var goals = await db.Goals
            .Where(g => g.WalletId == @event.WalletId && g.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (goals.Count == 0)
            return;

        // Recalculate from the current wallet balance for self-healing correctness
        var balance = await db.WalletBalances
            .Where(wb => wb.WalletId == @event.WalletId)
            .Select(wb => wb.Balance)
            .FirstOrDefaultAsync(cancellationToken);

        foreach (var goal in goals)
        {
            goal.CurrentProgress = balance;
            CheckMilestones(goal);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // Checks if the recalculated progress crosses any unrecorded milestone threshold.
    // Only fires each milestone once — subsequent drops and re-crosses are not re-fired.
    private void CheckMilestones(Goal goal)
    {
        if (goal.TargetAmount <= 0)
            return;

        foreach (var milestone in Milestones)
        {
            var threshold = goal.TargetAmount * milestone / 100m;
            if (goal.CurrentProgress >= threshold && goal.LastMilestone < milestone)
            {
                goal.LastMilestone = milestone;
                if (milestone == 100)
                {
                    eventBus.Publish(new GoalAchievedEvent
                    {
                        GoalId = goal.Id,
                        UserId = goal.UserId,
                        TargetAmount = goal.TargetAmount,
                        CurrentProgress = goal.CurrentProgress,
                    });
                }
                else
                {
                    eventBus.Publish(new GoalMilestoneReachedEvent
                    {
                        GoalId = goal.Id,
                        UserId = goal.UserId,
                        MilestonePercent = milestone,
                        CurrentProgress = goal.CurrentProgress,
                        TargetAmount = goal.TargetAmount,
                    });
                }
            }
        }
    }
}
