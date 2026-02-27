using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Store enum as string for readability and schema flexibility.
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .IsRequired();

        // Restrict deletes: wallet or category deletions must not cascade into transactions.
        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Wallet)
            .WithMany()
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DestinationWallet)
            .WithMany()
            .HasForeignKey(t => t.DestinationWalletId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Exclude transactions whose recorder is soft-deleted (mirrors UserConfiguration.HasQueryFilter).
        builder.HasQueryFilter(t => t.User!.DeletedAt == null);

        // Composite index for common query patterns (wallet history, date-ranged reports).
        builder.HasIndex(t => t.WalletId);
        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => new { t.WalletId, t.Date });
    }
}
