using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("settlements");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        // Cascade deletes wallet's settlements when the wallet is removed.
        builder.HasOne(s => s.Wallet)
            .WithMany()
            .HasForeignKey(s => s.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: user deletions must not silently cascade.
        builder.HasOne(s => s.FromUser)
            .WithMany()
            .HasForeignKey(s => s.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ToUser)
            .WithMany()
            .HasForeignKey(s => s.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for listing settlements by wallet.
        builder.HasIndex(s => s.WalletId);

        // Composite index for member pair lookups.
        builder.HasIndex(s => new { s.WalletId, s.FromUserId, s.ToUserId });
    }
}
