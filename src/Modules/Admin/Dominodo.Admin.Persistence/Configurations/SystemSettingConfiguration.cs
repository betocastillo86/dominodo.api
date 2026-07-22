using Dominodo.Admin.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Admin.Persistence.Configurations;

internal sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key).HasMaxLength(200).IsRequired();
        builder.Property(s => s.TenantId);

        // JSON-typed value (domain-model §4.4) — stored as nvarchar(max), interpreted by ValueType.
        builder.Property(s => s.Value).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(s => s.ValueType).HasConversion<int>().IsRequired();
        builder.Property(s => s.UpdatedAtUtc).IsRequired();

        // Unique per scope: one global row per key (TenantId null) and one override per (key, tenant).
        // HasFilter(null) removes EF's default "[TenantId] IS NOT NULL" filter so SQL Server enforces
        // uniqueness on the global rows too (it treats a single NULL as equal within a unique index).
        builder.HasIndex(s => new { s.Key, s.TenantId }).IsUnique().HasFilter(null);

        builder.Ignore(s => s.DomainEvents);
    }
}
