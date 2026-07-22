using Dominodo.Admin.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Admin.Persistence.Configurations;

internal sealed class DeviceRegistrationConfiguration : IEntityTypeConfiguration<DeviceRegistration>
{
    public void Configure(EntityTypeBuilder<DeviceRegistration> builder)
    {
        builder.ToTable("DeviceRegistrations");
        builder.HasKey(d => d.Id);

        // System-level (§4.3): keyed to UserId, no TenantId.
        builder.Property(d => d.UserId).IsRequired();
        builder.Property(d => d.Platform).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Token).HasMaxLength(512).IsRequired();
        builder.Property(d => d.IsActive).IsRequired();
        builder.Property(d => d.UpdatedAtUtc).IsRequired();

        // A device is identified by (UserId, Token) — re-registration upserts on this pair.
        builder.HasIndex(d => new { d.UserId, d.Token }).IsUnique();

        builder.Ignore(d => d.DomainEvents);
    }
}
