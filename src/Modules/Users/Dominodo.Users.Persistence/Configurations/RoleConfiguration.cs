using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Users.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();

        builder.Property(r => r.Description).HasMaxLength(300);
        builder.Property(r => r.IsSystem).IsRequired();

        // Role owns its RolePermission collection via the private backing field.
        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Role.Permissions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(r => r.Scope)
            .IsRequired()
            .HasConversion<int>();

        builder.HasData(UsersSeedData.Roles.Select(r => new
        {
            r.Id,
            r.Name,
            r.Description,
            r.IsSystem,
            r.Scope
        }));
    }
}
