using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Domain.Entities;

// Represents a user's membership in a shared wallet. CreatedAt serves as JoinedAt.
public sealed class WalletMember : Entity
{
    public required Guid WalletId { get; set; }
    public required Guid UserId { get; set; }

    // Navigation properties
    public Wallet? Wallet { get; set; }
    public User? User { get; set; }
}
