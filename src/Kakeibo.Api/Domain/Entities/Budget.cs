using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

// Represents a spending limit for a category over a time period.
// CurrentSpending is updated incrementally by event handlers (Phase 4b).
public sealed class Budget : Entity
{
    public required Guid UserId { get; set; }
    public required Guid CategoryId { get; set; }
    public required string Name { get; set; }
    public required decimal Limit { get; set; }
    public required LocalDate StartDate { get; set; }
    public required LocalDate EndDate { get; set; }

    // Null = monitor ALL user wallets; non-null = monitor specific wallet only.
    public Guid? WalletId { get; set; }

    // Updated incrementally by TransactionRecordedBudgetHandler, recalculated on update/delete.
    public decimal CurrentSpending { get; set; }

    // True when the budget was created by the seed-data endpoint (never set manually).
    public bool IsSeedData { get; set; }

    public User? User { get; set; }
    public Category? Category { get; set; }
    public Wallet? Wallet { get; set; }
}
