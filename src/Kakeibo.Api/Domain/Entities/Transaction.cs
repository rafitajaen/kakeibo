using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

public enum TransactionType
{
    Income,
    Expense,
    Transfer
}

// Records a financial event (income, expense, or transfer) that changes one or more wallet balances.
// Soft-deleted via DeletedAt; balance reversal happens at delete time.
public sealed class Transaction : Entity
{
    public required TransactionType Type { get; set; }
    public required decimal Amount { get; set; }
    public required string Description { get; set; }
    public required LocalDate Date { get; set; }
    public required Guid CategoryId { get; set; }
    public required Guid WalletId { get; set; }

    // Optional free-text note for additional context.
    public string? Notes { get; set; }

    // Populated only for Transfer transactions.
    public Guid? DestinationWalletId { get; set; }

    public required Guid UserId { get; set; }

    public Category? Category { get; set; }
    public Wallet? Wallet { get; set; }
    public Wallet? DestinationWallet { get; set; }
    public User? User { get; set; }
}
