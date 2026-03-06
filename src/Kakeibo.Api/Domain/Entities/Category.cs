using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Domain.Entities;

// Represents a transaction category.
// System categories (UserId = null) are seeded and cannot be modified or deleted.
// Custom categories are owned by a specific user (UserId is set).
public sealed class Category : Entity
{
    public required string Name { get; set; }

    // Null = system category (non-deletable, non-editable).
    // Non-null = custom category owned by the specified user.
    public Guid? UserId { get; set; }

    // Computed from UserId — not mapped to the database (see CategoryConfiguration.Ignore).
    public bool IsSystem => UserId is null;

    // True when the category was created by the seed-data endpoint (never set manually).
    public bool IsSeedData { get; set; }

    // Visual customization — only settable on custom categories; system categories use seeded defaults.
    public string? BackgroundColor { get; set; }  // hex color, e.g. "#EFF6FF"
    public string? TextColor { get; set; }         // hex color, e.g. "#1D4ED8"
    public string? Icon { get; set; }              // lucide icon name, e.g. "Home"

    public User? User { get; set; }
}
