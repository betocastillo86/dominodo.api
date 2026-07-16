using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Users.Persistence.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(UsersSeedData.RolePermissions.Select(rp => new
        {
            rp.RoleId,
            rp.PermissionId
        }));
    }
}
