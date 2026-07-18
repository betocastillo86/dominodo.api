using Dominodo.Tenants.Domain.Apartments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Tenants.Persistence.Configurations;

internal sealed class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.ToTable("Apartments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId).IsRequired();
        builder.HasIndex(a => a.TenantId);

        builder.Property(a => a.Tower).HasMaxLength(50);
        builder.Property(a => a.Number).HasMaxLength(50).IsRequired();

        // Unique within a tenant: (TenantId, Tower, Number). EF auto-filters nullable columns out of
        // unique indexes ("[Tower] IS NOT NULL"), which would let two null-tower "101"s coexist. Force
        // HasFilter(null) so null-tower rows are covered too — SQL Server treats NULLs as equal in a
        // unique index, so (tenant, NULL, "101") is still enforced unique.
        builder.HasIndex(a => new { a.TenantId, a.Tower, a.Number })
            .IsUnique()
            .HasFilter(null);

        builder.Property(a => a.Type).HasConversion<int>().IsRequired();
        builder.Property(a => a.Status).HasConversion<int>().IsRequired();

        builder.Property(a => a.Attributes).HasColumnType("nvarchar(max)");

        // Apartment owns its ApartmentResident collection via the private backing field (child entities,
        // mutated only through the aggregate).
        builder.HasMany(a => a.Residents)
            .WithOne()
            .HasForeignKey(r => r.ApartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Apartment.Residents))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(a => a.DomainEvents);
    }
}
