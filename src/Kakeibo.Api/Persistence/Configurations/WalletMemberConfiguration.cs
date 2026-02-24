using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class WalletMemberConfiguration : IEntityTypeConfiguration<WalletMember>
{
    public void Configure(EntityTypeBuilder<WalletMember> builder)
    {
        builder.ToTable("wallet_members");

        builder.HasKey(m => m.Id);

        // Each user can only be a member of a given wallet once
        builder.HasIndex(m => new { m.WalletId, m.UserId }).IsUnique();

        // Cascade from wallet: removing a wallet removes all member records
        builder.HasOne(m => m.Wallet)
            .WithMany(w => w.WalletMembers)
            .HasForeignKey(m => m.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict from user: prevents accidental cascade from user delete
        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
