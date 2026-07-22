using Dominodo.Operations.Domain.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Visits");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.TenantId).IsRequired();
        builder.HasIndex(v => v.TenantId);

        builder.Property(v => v.ApartmentId).IsRequired();
        builder.Property(v => v.Type).HasConversion<int>().IsRequired();
        builder.Property(v => v.Status).HasConversion<int>().IsRequired();

        builder.Property(v => v.VisitorName).HasMaxLength(200).IsRequired();
        builder.Property(v => v.VisitorDocument).HasMaxLength(50);
        builder.Property(v => v.PhotoUrl).HasMaxLength(2000);
        builder.Property(v => v.VehiclePlate).HasMaxLength(20);
        builder.Property(v => v.AmountPaid).HasColumnType("decimal(18,2)");
        builder.Property(v => v.RegisteredByUserId).IsRequired();
        builder.Property(v => v.AuthorizedByUserId);
        builder.Property(v => v.EntryAtUtc).IsRequired();
        builder.Property(v => v.ExitAtUtc);
        builder.Property(v => v.Metadata).HasColumnType("nvarchar(max)");

        // Shadow audit columns — AuditableEntityInterceptor sets CreatedAtUtc + UpdatedAtUtc.
        builder.Property<DateTimeOffset>("CreatedAtUtc");
        builder.Property<DateTimeOffset>("UpdatedAtUtc");

        // Filterable columns (domain-model §5.1): status + destination apartment drive list queries.
        builder.HasIndex(v => new { v.TenantId, v.Status });
        builder.HasIndex(v => new { v.TenantId, v.ApartmentId });

        builder.Ignore(v => v.DomainEvents);
    }
}
