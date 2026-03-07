using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Domain.Entities;

public enum WalletMemberRole
{
    Owner,
    Editor,
    Guest
}

// Represents a user's membership in a wallet with a specific role. CreatedAt serves as JoinedAt.
public sealed class WalletMember : Entity
{
    public required Guid WalletId { get; set; }
    public required Guid UserId { get; set; }
    public WalletMemberRole Role { get; set; } = WalletMemberRole.Guest;

    // Navigation properties
    public Wallet? Wallet { get; set; }
    public User? User { get; set; }
}
