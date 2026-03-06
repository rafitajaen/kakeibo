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

    // Lucide icon name (e.g. "Wallet", "PiggyBank"). Null = use default icon.
    public string? Icon { get; set; }

    // Tailwind hex or token for the card background (e.g. "#3B82F6").
    public string? BackgroundColor { get; set; }

    // Text color for contrast on the card (e.g. "#FFFFFF").
    public string? TextColor { get; set; }

    // True when the wallet was created by the seed-data endpoint (never set manually).
    public bool IsSeedData { get; set; }

    // Navigation properties
    public User? Owner { get; set; }
    public ICollection<WalletMember> WalletMembers { get; set; } = [];
    public WalletBalance? WalletBalance { get; set; }
}
