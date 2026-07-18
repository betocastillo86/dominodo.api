using Dominodo.Tenants.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Tenants.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.Slug).IsUnique();

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.LegalId).HasMaxLength(50);

        builder.Property(t => t.Type).HasConversion<int>().IsRequired();
        builder.Property(t => t.Status).HasConversion<int>().IsRequired();

        builder.Property(t => t.Address).HasMaxLength(300).IsRequired();
        builder.Property(t => t.City).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Country).HasMaxLength(100).IsRequired();

        builder.Property(t => t.Branding).HasColumnType("nvarchar(max)");
        builder.Property(t => t.Settings).HasColumnType("nvarchar(max)");

        builder.Property<DateTimeOffset>("CreatedAtUtc");
        builder.Property<DateTimeOffset>("UpdatedAtUtc");

        // Tenant owns its TenantFeature collection via the private backing field (child entities,
        // mutated only through the aggregate).
        builder.HasMany(t => t.Features)
            .WithOne()
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Tenant.Features))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.DomainEvents);
    }
}
