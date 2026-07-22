using Dominodo.Operations.Domain.Deliveries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("Deliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantId).IsRequired();
        builder.HasIndex(d => d.TenantId);

        builder.Property(d => d.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(d => new { d.TenantId, d.Code }).IsUnique();

        builder.Property(d => d.ApartmentId).IsRequired();
        builder.Property(d => d.Type).HasConversion<int>().IsRequired();
        builder.Property(d => d.Status).HasConversion<int>().IsRequired();
        builder.Property(d => d.RegisteredByUserId).IsRequired();

        builder.Property(d => d.PhotoUrl).HasMaxLength(2000);
        builder.Property(d => d.Comment).HasMaxLength(1000);
        builder.Property(d => d.Carrier).HasMaxLength(100);
        builder.Property(d => d.ReceivedAtUtc).IsRequired();
        builder.Property(d => d.DeliveredAtUtc);
        builder.Property(d => d.ReceivedByName).HasMaxLength(200);
        builder.Property(d => d.DeliveredToUserId);
        builder.Property(d => d.Metadata).HasColumnType("nvarchar(max)");

        // Shadow audit columns — AuditableEntityInterceptor sets CreatedAtUtc + UpdatedAtUtc.
        builder.Property<DateTimeOffset>("CreatedAtUtc");
        builder.Property<DateTimeOffset>("UpdatedAtUtc");

        // Filterable columns (domain-model §5.1): status + destination apartment drive list queries.
        builder.HasIndex(d => new { d.TenantId, d.Status });
        builder.HasIndex(d => new { d.TenantId, d.ApartmentId });

        builder.Ignore(d => d.DomainEvents);
    }
}
