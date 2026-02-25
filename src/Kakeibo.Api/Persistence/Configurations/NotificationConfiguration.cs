using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Body)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(n => n.Metadata)
            .HasMaxLength(2000);

        // Cascade deletes user's notifications when the user is removed.
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for loading notifications by user and read status.
        builder.HasIndex(n => new { n.UserId, n.IsRead });

        // Soft-delete filter — only return non-deleted notifications.
        builder.HasQueryFilter(n => n.DeletedAt == null);
    }
}
