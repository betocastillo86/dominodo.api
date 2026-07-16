using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Users.Persistence.Configurations;

internal sealed class PlatformRoleAssignmentConfiguration : IEntityTypeConfiguration<PlatformRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PlatformRoleAssignment> builder)
    {
        builder.ToTable("PlatformRoleAssignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.RoleId).IsRequired();

        // A user may be assigned the same platform role only once.
        builder.HasIndex(a => new { a.UserId, a.RoleId }).IsUnique();
        builder.HasIndex(a => a.UserId);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(a => a.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(UsersSeedData.PlatformRoleAssignments.Select(a => new
        {
            a.Id,
            a.UserId,
            a.RoleId
        }));
    }
}
