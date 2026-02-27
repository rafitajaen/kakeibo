using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Budgets.Events;

// Increments CurrentSpending on matching budgets when an Expense transaction is recorded.
// Publishes BudgetWarningEvent (75%) or BudgetExceededEvent (100%) when thresholds are crossed.
public sealed class TransactionRecordedBudgetHandler(AppDbContext db, IEventBus eventBus)
    : IEventHandler<TransactionRecordedEvent>
{
    public async Task HandleAsync(TransactionRecordedEvent @event, CancellationToken cancellationToken = default)
    {
        // Only Expense transactions affect budget spending
        if (@event.Type != "Expense")
        {
            return;
        }

        var budgets = await db.Budgets
            .Where(b =>
                b.UserId == @event.UserId
                && b.CategoryId == @event.CategoryId
                && b.StartDate <= @event.Date
                && b.EndDate >= @event.Date
                && (b.WalletId == null || b.WalletId == @event.WalletId)
                && b.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var budget in budgets)
        {
            var oldSpending = budget.CurrentSpending;
            budget.CurrentSpending += @event.Amount;
            var newSpending = budget.CurrentSpending;

            var warningThreshold = budget.Limit * 0.75m;

            // Publish warning when crossing the 75% threshold for the first time
            if (oldSpending < warningThreshold && newSpending >= warningThreshold)
            {
                eventBus.Publish(new BudgetWarningEvent
                {
                    BudgetId = budget.Id,
                    UserId = budget.UserId,
                    CategoryId = budget.CategoryId,
                    Limit = budget.Limit,
                    CurrentSpending = newSpending,
                    PercentUsed = newSpending / budget.Limit * 100m,
                });
            }

            // Publish exceeded when crossing the 100% threshold for the first time
            if (oldSpending < budget.Limit && newSpending >= budget.Limit)
            {
                eventBus.Publish(new BudgetExceededEvent
                {
                    BudgetId = budget.Id,
                    UserId = budget.UserId,
                    CategoryId = budget.CategoryId,
                    Limit = budget.Limit,
                    CurrentSpending = newSpending,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
