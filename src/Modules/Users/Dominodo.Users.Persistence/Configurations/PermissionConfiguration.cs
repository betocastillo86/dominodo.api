using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Users.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Description).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Group).HasMaxLength(100).IsRequired();

        builder.HasData(UsersSeedData.Permissions.Select(p => new
        {
            p.Id,
            p.Code,
            p.Description,
            p.Group
        }));
    }
}
