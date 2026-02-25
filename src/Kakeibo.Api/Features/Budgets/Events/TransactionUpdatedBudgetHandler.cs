using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Budgets.Events;

// Recalculates CurrentSpending for budgets affected by a transaction update.
// Handles both old and new category to cover category-change scenarios.
// Also publishes threshold alerts when spending levels change.
public sealed class TransactionUpdatedBudgetHandler(AppDbContext db, IEventBus eventBus)
    : IEventHandler<TransactionUpdatedEvent>
{
    public async Task HandleAsync(TransactionUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event.Type != "Expense")
            return;

        // Collect unique category IDs that may have affected budgets (old + new)
        var categoryIds = new HashSet<Guid> { @event.CategoryId, @event.OldCategoryId };

        var budgets = await db.Budgets
            .Where(b =>
                b.UserId == @event.UserId
                && categoryIds.Contains(b.CategoryId)
                && b.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var budget in budgets)
        {
            var oldSpending = budget.CurrentSpending;

            // Recalculate from DB to ensure correctness
            budget.CurrentSpending = await db.Transactions
                .Where(t =>
                    t.UserId == budget.UserId
                    && t.Type == TransactionType.Expense
                    && t.CategoryId == budget.CategoryId
                    && t.Date >= budget.StartDate
                    && t.Date <= budget.EndDate
                    && (budget.WalletId == null || t.WalletId == budget.WalletId)
                    && t.DeletedAt == null)
                .SumAsync(t => t.Amount, cancellationToken);

            var newSpending = budget.CurrentSpending;
            var warningThreshold = budget.Limit * 0.75m;

            // Publish warning if threshold newly crossed
            if (oldSpending < warningThreshold && newSpending >= warningThreshold)
            {
                eventBus.Publish(new BudgetWarningEvent
                {
                    BudgetId = budget.Id,
                    UserId = budget.UserId,
                    CategoryId = budget.CategoryId,
                    Limit = budget.Limit,
                    CurrentSpending = newSpending,
                    PercentUsed = budget.Limit > 0 ? (newSpending / budget.Limit) * 100m : 0m,
                });
            }

            // Publish exceeded if threshold newly crossed
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
