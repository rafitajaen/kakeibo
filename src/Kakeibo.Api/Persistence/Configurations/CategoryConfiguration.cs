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

        // A partial unique index on (name, user_id) WHERE deleted_at IS NULL is added via
        // migrationBuilder.Sql() in the migration to handle archived categories correctly.

        // Seed the 12 immutable system categories with stable GUIDs and timestamps.
        builder.HasData(
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Housing"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Transportation"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Food & Dining"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Health & Wellness"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Entertainment & Leisure"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000006"), "Shopping & Personal"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000007"), "Education"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000008"), "Subscriptions & Bills"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-000000000009"), "Savings & Investments"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-00000000000a"), "Debt & Loans"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-00000000000b"), "Gifts & Donations"),
            CreateSystem(Guid.Parse("10000000-0000-0000-0000-00000000000c"), "Other"));
    }

    // Builds a system category seed record with a stable ID, name, and timestamps.
    private static Category CreateSystem(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        UserId = null,
        CreatedAt = SeedInstant,
        UpdatedAt = SeedInstant
    };
}
