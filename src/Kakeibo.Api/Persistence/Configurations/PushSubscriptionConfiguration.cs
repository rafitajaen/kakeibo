using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Endpoint)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(ps => ps.P256dh)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ps => ps.Auth)
            .HasMaxLength(100)
            .IsRequired();

        // Cascade deletes user's subscriptions when the user is removed.
        builder.HasOne(ps => ps.User)
            .WithMany()
            .HasForeignKey(ps => ps.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Exclude subscriptions whose owner is soft-deleted (mirrors UserConfiguration.HasQueryFilter).
        builder.HasQueryFilter(ps => ps.User!.DeletedAt == null);

        // Index for loading subscriptions by user.
        builder.HasIndex(ps => ps.UserId);
    }
}
