using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Store enum as string for readability and schema flexibility
        builder.Property(w => w.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // Restrict delete: prevents accidental cascade from User delete
        builder.HasOne(w => w.Owner)
            .WithMany()
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
