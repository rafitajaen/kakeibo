using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Budgets.Events;

// Recalculates CurrentSpending for matching budgets when an Expense transaction is deleted.
// Uses a full SUM recalculation rather than subtraction for self-healing correctness.
public sealed class TransactionDeletedBudgetHandler(AppDbContext db)
    : IEventHandler<TransactionDeletedEvent>
{
    public async Task HandleAsync(TransactionDeletedEvent @event, CancellationToken cancellationToken = default)
    {
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
            // Recalculate from DB to ensure correctness even if prior state was inconsistent
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
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
