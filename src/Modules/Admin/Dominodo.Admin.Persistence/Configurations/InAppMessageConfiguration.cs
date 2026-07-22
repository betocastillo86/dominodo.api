using Dominodo.Admin.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Admin.Persistence.Configurations;

internal sealed class InAppMessageConfiguration : IEntityTypeConfiguration<InAppMessage>
{
    public void Configure(EntityTypeBuilder<InAppMessage> builder)
    {
        builder.ToTable("InAppMessages");
        builder.HasKey(n => n.Id);

        // TenantId is a plain column (NOT ITenantOwned) — queried by recipient (§4.2).
        builder.Property(n => n.TenantId).IsRequired();
        builder.Property(n => n.RecipientUserId).IsRequired();
        builder.Property(n => n.Type).HasConversion<int>().IsRequired();
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(n => n.TargetUrl).HasMaxLength(2048);
        builder.Property(n => n.IsRead).IsRequired();
        builder.Property(n => n.ReadAtUtc);
        builder.Property(n => n.TriggeredByUserId);
        builder.Property(n => n.CreatedAtUtc).IsRequired();

        // Shadow audit column: AuditableEntityInterceptor sets CreatedAtUtc + UpdatedAtUtc as a pair on
        // insert for any entity exposing CreatedAtUtc. CreatedAtUtc is domain data here (§4.2), so it
        // stays a real property; UpdatedAtUtc is infra-only and kept as a shadow property.
        builder.Property<DateTimeOffset>("UpdatedAtUtc");

        builder.HasIndex(n => n.RecipientUserId);
        builder.HasIndex(n => n.TenantId);

        builder.Ignore(n => n.DomainEvents);
    }
}
