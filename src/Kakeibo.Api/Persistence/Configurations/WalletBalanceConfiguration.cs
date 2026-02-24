using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class WalletBalanceConfiguration : IEntityTypeConfiguration<WalletBalance>
{
    public void Configure(EntityTypeBuilder<WalletBalance> builder)
    {
        builder.ToTable("wallet_balances");

        // WalletId is the primary key — 1:1 with Wallet, no separate identity column.
        builder.HasKey(wb => wb.WalletId);

        builder.Property(wb => wb.Balance)
            .HasPrecision(18, 2);

        // Cascade delete: removing a wallet also removes its balance record.
        builder.HasOne(wb => wb.Wallet)
            .WithOne(w => w.WalletBalance)
            .HasForeignKey<WalletBalance>(wb => wb.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
