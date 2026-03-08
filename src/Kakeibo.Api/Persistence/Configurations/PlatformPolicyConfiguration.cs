using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kakeibo.Api.Persistence.Configurations;

public sealed class PlatformPolicyConfiguration : IEntityTypeConfiguration<PlatformPolicy>
{
    public void Configure(EntityTypeBuilder<PlatformPolicy> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RegistrationEnabled)
            .IsRequired();

        builder.Property(p => p.MaintenanceMode)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();
    }
}
