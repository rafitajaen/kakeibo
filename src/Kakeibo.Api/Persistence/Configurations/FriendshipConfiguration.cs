using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships");

        builder.HasKey(f => f.Id);

        builder.HasIndex(f => new { f.UserAId, f.UserBId })
            .IsUnique();

        builder.HasOne(f => f.UserA)
            .WithMany()
            .HasForeignKey(f => f.UserAId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.UserB)
            .WithMany()
            .HasForeignKey(f => f.UserBId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(f => f.DeletedAt == null);
    }
}
