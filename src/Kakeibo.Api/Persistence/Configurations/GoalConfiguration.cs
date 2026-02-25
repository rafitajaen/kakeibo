using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("goals");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.TargetAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(g => g.CurrentProgress)
            .HasPrecision(18, 2);

        // Cascade deletes user's goals when the user is removed.
        builder.HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: wallet deletions must not silently cascade into goals.
        builder.HasOne(g => g.Wallet)
            .WithMany()
            .HasForeignKey(g => g.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for listing goals by user.
        builder.HasIndex(g => g.UserId);

        // Composite index for wallet + user — used in event handlers to find affected goals.
        builder.HasIndex(g => new { g.WalletId, g.UserId });
    }
}
