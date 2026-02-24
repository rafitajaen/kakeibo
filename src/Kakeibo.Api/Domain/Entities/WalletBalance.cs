using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

// Tracks the current balance for a wallet.
// Owned by the Transactions domain — updated atomically in the same SaveChangesAsync() call as the transaction.
// Uses WalletId as the primary key (1:1 with Wallet). Does not inherit Entity (no soft delete needed).
public sealed class WalletBalance
{
    public required Guid WalletId { get; set; }
    public decimal Balance { get; set; }
    public Instant UpdatedAt { get; set; } = SystemClock.Instance.GetCurrentInstant();

    public Wallet? Wallet { get; set; }
}
