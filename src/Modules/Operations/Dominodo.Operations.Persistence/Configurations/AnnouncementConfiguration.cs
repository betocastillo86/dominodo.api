using Dominodo.Operations.Domain.Announcements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Operations.Persistence.Configurations;

internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId).IsRequired();
        builder.HasIndex(a => a.TenantId);

        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Body).IsRequired();
        builder.Property(a => a.Category).HasMaxLength(100);
        builder.Property(a => a.Priority).IsRequired();
        builder.Property(a => a.PublishedAtUtc);
        builder.Property(a => a.ExpiresAtUtc);
        builder.Property(a => a.AudienceType).HasConversion<int>().IsRequired();
        builder.Property(a => a.AudienceFilter).HasColumnType("nvarchar(max)");
        builder.Property(a => a.Status).HasConversion<int>().IsRequired();
        builder.Property(a => a.PublishedByUserId);

        // Shadow audit columns — AuditableEntityInterceptor sets CreatedAtUtc + UpdatedAtUtc.
        builder.Property<DateTimeOffset>("CreatedAtUtc");
        builder.Property<DateTimeOffset>("UpdatedAtUtc");

        // Filterable columns (domain-model §5.1): Category (free tenant filter) and status drive queries.
        builder.HasIndex(a => a.Category);
        builder.HasIndex(a => new { a.TenantId, a.Status });

        builder.HasMany(a => a.Attachments)
            .WithOne()
            .HasForeignKey(x => x.AnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Announcement.Attachments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(a => a.DomainEvents);
    }
}
