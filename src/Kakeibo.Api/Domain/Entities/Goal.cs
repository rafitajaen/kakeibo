using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

// Represents a savings target linked to a specific wallet.
// CurrentProgress mirrors WalletBalance.Balance of the linked wallet.
// LastMilestone tracks the highest milestone reached (0, 25, 50, 75, 100)
// so duplicate milestone events are not emitted when progress fluctuates.
public sealed class Goal : Entity
{
    public required Guid UserId { get; set; }
    public required string Name { get; set; }          // max 100 chars
    public required decimal TargetAmount { get; set; } // 0.01–999,999,999.99
    public LocalDate? Deadline { get; set; }           // optional, max 10 years in future
    public required Guid WalletId { get; set; }        // wallet-linked tracking (MVP)

    // Cache of WalletBalance.Balance for the linked wallet — updated by event handlers.
    public decimal CurrentProgress { get; set; }

    // Highest milestone percentage already reached (0, 25, 50, 75, 100).
    // Prevents duplicate milestone events when progress temporarily drops and re-crosses.
    public int LastMilestone { get; set; }

    // True when the goal was created by the seed-data endpoint (never set manually).
    public bool IsSeedData { get; set; }

    public User? User { get; set; }
    public Wallet? Wallet { get; set; }
}
