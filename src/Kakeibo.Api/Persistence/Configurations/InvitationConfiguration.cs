using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InviteeEmail)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(i => i.Code)
            .HasMaxLength(32)
            .IsRequired();

        // Unique index so code lookups are fast and duplicates are prevented at the DB level
        builder.HasIndex(i => i.Code).IsUnique();

        // Ignore computed properties — EF cannot persist expression-bodied booleans (TD-016)
        builder.Ignore(i => i.IsAccepted);
        builder.Ignore(i => i.IsRevoked);

        builder.HasOne(i => i.Wallet)
            .WithMany()
            .HasForeignKey(i => i.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Inviter)
            .WithMany()
            .HasForeignKey(i => i.InviterUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
