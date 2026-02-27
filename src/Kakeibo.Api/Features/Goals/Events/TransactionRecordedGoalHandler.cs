using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Goals.Events;

// Updates CurrentProgress for all active goals linked to the transaction's wallet.
// Responds to all transaction types (Income, Expense, Transfer) since all affect wallet balance.
// Reads WalletBalance.Balance directly for self-healing correctness.
// Publishes GoalMilestoneReachedEvent (25/50/75%) or GoalAchievedEvent (100%)
// when progress first crosses a milestone threshold.
public sealed class TransactionRecordedGoalHandler(AppDbContext db, IEventBus eventBus)
    : IEventHandler<TransactionRecordedEvent>
{
    private static readonly int[] Milestones = [25, 50, 75, 100];

    public async Task HandleAsync(TransactionRecordedEvent @event, CancellationToken cancellationToken = default)
    {
        var goals = await db.Goals
            .Where(g => g.WalletId == @event.WalletId && g.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (goals.Count == 0)
        {
            return;
        }

        // Read the current wallet balance (already updated atomically with the transaction)
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

    // Checks if the current progress crosses any unrecorded milestone threshold.
    // Only fires each milestone once — subsequent drops and re-crosses are not re-fired.
    private void CheckMilestones(Goal goal)
    {
        if (goal.TargetAmount <= 0)
        {
            return;
        }

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
