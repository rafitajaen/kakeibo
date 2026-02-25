using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

public enum RecurrenceFrequency
{
    Daily,
    Weekly,
    Biweekly,
    Monthly,
    Yearly
}

// Defines a repeating transaction template that the background job uses to auto-generate transactions.
// IsActive is derived: pattern is active when not soft-deleted and NextOccurrence is within the EndDate window.
public sealed class RecurringPattern : Entity
{
    public required Guid UserId { get; set; }
    public required string Name { get; set; }              // max 100 chars
    public required decimal Amount { get; set; }           // 0.01–999,999,999.99
    public required string Description { get; set; }       // max 500 chars
    public required TransactionType TransactionType { get; set; }
    public required RecurrenceFrequency Frequency { get; set; }
    public required Guid CategoryId { get; set; }
    public required Guid WalletId { get; set; }

    // Populated only for Transfer patterns.
    public Guid? DestinationWalletId { get; set; }

    public required LocalDate StartDate { get; set; }
    public LocalDate? EndDate { get; set; }

    // Next date this pattern should generate a transaction.
    // Initialized to StartDate at creation; advanced by RecurrenceCalculator after each generation.
    public required LocalDate NextOccurrence { get; set; }

    // Timestamp of the most recent successful generation run; null if never generated.
    public Instant? LastGeneratedAt { get; set; }

    // Active when not soft-deleted and NextOccurrence is still within the EndDate window.
    public bool IsActive => DeletedAt is null && (EndDate is null || NextOccurrence <= EndDate);

    public User? User { get; set; }
    public Category? Category { get; set; }
    public Wallet? Wallet { get; set; }
    public Wallet? DestinationWallet { get; set; }
}
