using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(u => u.Name)
            .HasMaxLength(100);

        builder.Property(u => u.EmailVerificationToken)
            .HasMaxLength(64);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(u => u.WeekStartDay)
            .HasDefaultValue(1);

        builder.Property(u => u.MonthStartDay)
            .HasDefaultValue(1);

        builder.Property(u => u.CurrencyDecimalSeparator)
            .HasMaxLength(1)
            .HasDefaultValue(".");

        builder.Property(u => u.CurrencyGroupSeparator)
            .HasMaxLength(1)
            .HasDefaultValue(",");

        builder.Property(u => u.CurrencySymbolPosition)
            .HasMaxLength(6)
            .HasDefaultValue("before");

        builder.Property(u => u.CurrencyDisplay)
            .HasMaxLength(6)
            .HasDefaultValue("symbol");

        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
