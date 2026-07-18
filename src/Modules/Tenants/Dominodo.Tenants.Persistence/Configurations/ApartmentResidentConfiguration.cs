using Dominodo.Tenants.Domain.Apartments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Tenants.Persistence.Configurations;

internal sealed class ApartmentResidentConfiguration : IEntityTypeConfiguration<ApartmentResident>
{
    public void Configure(EntityTypeBuilder<ApartmentResident> builder)
    {
        builder.ToTable("ApartmentResidents");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.ApartmentId).IsRequired();
        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.UserId).IsRequired();

        builder.Property(r => r.RelationType).HasConversion<int>().IsRequired();
        builder.Property(r => r.LivesHere).IsRequired();
        builder.Property(r => r.StartDate);
        builder.Property(r => r.EndDate);
        builder.Property(r => r.IsActive).IsRequired();

        // One ACTIVE residency per (apartment, user) — enforced at the DB level too (the domain also
        // guards it). Ended rows are kept for history and excluded by the filter, so a user can re-join.
        // Multi-owner is unaffected: distinct users each get their own active row.
        builder.HasIndex(r => new { r.ApartmentId, r.UserId })
            .IsUnique()
            .HasFilter("[IsActive] = 1");
    }
}
