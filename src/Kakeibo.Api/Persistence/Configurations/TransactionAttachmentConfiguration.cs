using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class TransactionAttachmentConfiguration : IEntityTypeConfiguration<TransactionAttachment>
{
    public void Configure(EntityTypeBuilder<TransactionAttachment> builder)
    {
        builder.ToTable("transaction_attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.FileSizeBytes)
            .IsRequired();

        builder.Property(a => a.ObjectName)
            .HasMaxLength(500)
            .IsRequired();

        // Cascade delete: removing a transaction removes its attachment metadata.
        // The MinIO files are cleaned up via a background job (deferred cleanup for soft-deletes).
        builder.HasOne(a => a.Transaction)
            .WithMany()
            .HasForeignKey(a => a.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: deleting a user does not remove attachment records (audit trail).
        builder.HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fast lookup of all attachments for a transaction.
        builder.HasIndex(a => a.TransactionId);

        // Mirror Transaction's query filter so direct attachment queries also exclude soft-deleted users.
        builder.HasQueryFilter(a => a.Transaction!.User!.DeletedAt == null);
    }
}
