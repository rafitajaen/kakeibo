using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Domain.Entities;

public enum WalletType
{
    Personal,
    Shared
}

// A financial container that holds money and organizes transactions.
public sealed class Wallet : Entity
{
    public required string Name { get; set; }
    public WalletType Type { get; set; } = WalletType.Personal;
    public required Guid OwnerId { get; set; }
    public required string Currency { get; set; }

    // Navigation properties
    public User? Owner { get; set; }
    public ICollection<WalletMember> WalletMembers { get; set; } = [];
    public WalletBalance? WalletBalance { get; set; }
}
