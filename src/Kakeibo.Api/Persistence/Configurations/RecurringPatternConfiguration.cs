using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class RecurringPatternConfiguration : IEntityTypeConfiguration<RecurringPattern>
{
    public void Configure(EntityTypeBuilder<RecurringPattern> builder)
    {
        builder.ToTable("recurring_patterns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Store enums as strings for readability in the database.
        builder.Property(r => r.TransactionType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.Frequency)
            .HasConversion<string>()
            .IsRequired();

        // Cascade deletes user's recurring patterns when the user is removed.
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: category/wallet deletions must not silently cascade into patterns.
        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Wallet)
            .WithMany()
            .HasForeignKey(r => r.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional destination wallet for Transfer patterns.
        builder.HasOne(r => r.DestinationWallet)
            .WithMany()
            .HasForeignKey(r => r.DestinationWalletId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Index for listing patterns by user.
        builder.HasIndex(r => r.UserId);

        // Index for NextOccurrence — the Hangfire job queries patterns due today.
        builder.HasIndex(r => r.NextOccurrence);

        // Compound index for the most common job query pattern: active patterns due by a date.
        builder.HasIndex(r => new { r.UserId, r.NextOccurrence });
    }
}
