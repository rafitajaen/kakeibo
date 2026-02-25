using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

// Records an external debt payment between two wallet members.
// Settlements do not affect wallet balance — money is transferred outside the platform.
public sealed class Settlement : Entity
{
    public required Guid WalletId { get; set; }
    public required Guid FromUserId { get; set; }
    public required Guid ToUserId { get; set; }
    public required decimal Amount { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Wallet? Wallet { get; set; }
    public User? FromUser { get; set; }
    public User? ToUser { get; set; }
}
