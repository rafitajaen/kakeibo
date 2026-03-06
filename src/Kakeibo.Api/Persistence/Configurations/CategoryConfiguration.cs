using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    // Fixed instant used for all system category seed records.
    // Must remain stable — changing it would generate a spurious migration.
    private static readonly Instant SeedInstant = Instant.FromUtc(2026, 1, 1, 0, 0);

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(50)
            .IsRequired();

        // UserId is null for system categories; set for user-created custom categories.
        builder.Property(c => c.UserId)
            .IsRequired(false);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // IsSystem is derived from UserId — it has no backing column.
        builder.Ignore(c => c.IsSystem);

        builder.Property(c => c.BackgroundColor)
            .HasMaxLength(7);

        builder.Property(c => c.TextColor)
            .HasMaxLength(7);

        builder.Property(c => c.Icon)
            .HasMaxLength(50);

        // A partial unique index on (name, user_id) WHERE deleted_at IS NULL is added via
        // migrationBuilder.Sql() in the migration to handle archived categories correctly.

        // Seed the 12 immutable system categories with stable GUIDs, timestamps, and visual defaults.
        builder.HasData(
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Housing",               "Home",            "#EFF6FF", "#1D4ED8"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Transportation",        "Car",             "#F0FDF4", "#15803D"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Food & Dining",         "UtensilsCrossed", "#FFF7ED", "#C2410C"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Health & Wellness",     "Heart",           "#FFF1F2", "#BE123C"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Entertainment & Leisure","Tv",             "#FAF5FF", "#7E22CE"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000006"), "Shopping & Personal",   "ShoppingCart",    "#FDF2F8", "#9D174D"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000007"), "Education",             "BookOpen",        "#EEF2FF", "#3730A3"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000008"), "Subscriptions & Bills", "Repeat",          "#F0FDFA", "#0F766E"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000009"), "Savings & Investments", "PiggyBank",       "#ECFDF5", "#065F46"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-00000000000a"), "Debt & Loans",          "CreditCard",      "#FEFCE8", "#854D0E"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-00000000000b"), "Gifts & Donations",     "Gift",            "#FFF1F2", "#9F1239"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-00000000000c"), "Other",                 "MoreHorizontal",  "#F9FAFB", "#374151"));
    }

    // Builds a system category seed record with stable ID, name, timestamps, and visual defaults.
    private static Category CreateSystem(Guid id, string name, string icon, string bgColor, string textColor) => new()
    {
        Id = id,
        Name = name,
        UserId = null,
        CreatedAt = SeedInstant,
        UpdatedAt = SeedInstant,
        Icon = icon,
        BackgroundColor = bgColor,
        TextColor = textColor
    };
}
