using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(prt => prt.Id);

        builder.Property(prt => prt.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(prt => prt.TokenHash)
            .IsUnique();

        builder.HasOne(prt => prt.User)
            .WithMany()
            .HasForeignKey(prt => prt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Exclude tokens whose owner is soft-deleted (mirrors UserConfiguration.HasQueryFilter).
        builder.HasQueryFilter(prt => prt.User!.DeletedAt == null);
    }
}
