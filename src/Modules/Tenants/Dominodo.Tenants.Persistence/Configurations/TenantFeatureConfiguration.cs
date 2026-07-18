using Dominodo.Tenants.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Tenants.Persistence.Configurations;

internal sealed class TenantFeatureConfiguration : IEntityTypeConfiguration<TenantFeature>
{
    public void Configure(EntityTypeBuilder<TenantFeature> builder)
    {
        builder.ToTable("TenantFeatures");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.TenantId).IsRequired();
        builder.Property(f => f.FeatureKey).HasConversion<int>().IsRequired();
        builder.Property(f => f.Enabled).IsRequired();

        // One row per (tenant, feature) — enabling is an idempotent upsert (domain guarantees it too).
        builder.HasIndex(f => new { f.TenantId, f.FeatureKey }).IsUnique();
    }
}
