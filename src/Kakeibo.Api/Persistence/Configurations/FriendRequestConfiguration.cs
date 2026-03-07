using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.ToTable("friend_requests");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.SenderUserId, r.ReceiverUserId })
            .IsUnique();

        builder.HasOne(r => r.Sender)
            .WithMany()
            .HasForeignKey(r => r.SenderUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Receiver)
            .WithMany()
            .HasForeignKey(r => r.ReceiverUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
