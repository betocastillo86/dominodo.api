using Dominodo.Operations.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("Requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId).IsRequired();
        builder.HasIndex(r => r.TenantId);

        builder.Property(r => r.Code).HasMaxLength(30).IsRequired();
        // Readable code is unique within a tenant (per-tenant sequence, domain-model §5.2).
        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique();

        builder.Property(r => r.Type).HasConversion<int>().IsRequired();
        builder.Property(r => r.Status).HasConversion<int>().IsRequired();
        builder.Property(r => r.Priority).HasConversion<int>().IsRequired();

        builder.Property(r => r.Category).HasMaxLength(100);
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).IsRequired();
        builder.Property(r => r.Location).HasMaxLength(200);

        builder.Property(r => r.CreatedByUserId).IsRequired();
        builder.Property(r => r.ApartmentId);
        builder.Property(r => r.AssignedToUserId);
        builder.Property(r => r.Metadata).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ResolvedAtUtc);
        builder.Property(r => r.ClosedAtUtc);

        // Shadow audit columns — AuditableEntityInterceptor sets CreatedAtUtc + UpdatedAtUtc.
        builder.Property<DateTimeOffset>("CreatedAtUtc");
        builder.Property<DateTimeOffset>("UpdatedAtUtc");

        // Filterable columns (domain-model §5.1): status/type/priority/assignee/apartment drive list queries.
        builder.HasIndex(r => new { r.TenantId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.ApartmentId });
        builder.HasIndex(r => new { r.TenantId, r.AssignedToUserId });

        // Child collections owned through the aggregate via private backing fields.
        builder.HasMany(r => r.Participants)
            .WithOne()
            .HasForeignKey(p => p.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Updates)
            .WithOne()
            .HasForeignKey(u => u.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Attachments)
            .WithOne()
            .HasForeignKey(a => a.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Request.Participants))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Request.Updates))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Request.Attachments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Request.StatusHistory))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.DomainEvents);
    }
}
