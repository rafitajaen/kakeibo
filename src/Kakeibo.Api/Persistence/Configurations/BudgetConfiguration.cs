using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.Limit)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(b => b.CurrentSpending)
            .HasPrecision(18, 2);

        // Cascade deletes user's budgets when the user is removed.
        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: category deletions must not silently cascade into budgets.
        builder.HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional wallet filter — restrict to avoid accidental cascade.
        builder.HasOne(b => b.Wallet)
            .WithMany()
            .HasForeignKey(b => b.WalletId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Composite indexes for common query patterns.
        builder.HasIndex(b => new { b.UserId, b.CategoryId });
        builder.HasIndex(b => new { b.UserId, b.StartDate, b.EndDate });
    }
}
