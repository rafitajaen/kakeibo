using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Domain.Entities;

public enum WalletType
{
    Personal,
    Shared
}

public enum WalletVisibility
{
    Private,
    Public
}

// A financial container that holds money and organizes transactions.
public sealed class Wallet : Entity
{
    public required string Name { get; set; }
    public WalletType Type { get; set; } = WalletType.Personal;
    public required string Currency { get; set; }
    public WalletVisibility Visibility { get; set; } = WalletVisibility.Private;

    // Lucide icon name (e.g. "Wallet", "PiggyBank"). Null = use default icon.
    public string? Icon { get; set; }

    // Tailwind hex or token for the card background (e.g. "#3B82F6").
    public string? BackgroundColor { get; set; }

    // Text color for contrast on the card (e.g. "#FFFFFF").
    public string? TextColor { get; set; }

    // True when the wallet was created by the seed-data endpoint (never set manually).
    public bool IsSeedData { get; set; }

    // Navigation properties
    public ICollection<WalletMember> WalletMembers { get; set; } = [];
    public WalletBalance? WalletBalance { get; set; }
}
