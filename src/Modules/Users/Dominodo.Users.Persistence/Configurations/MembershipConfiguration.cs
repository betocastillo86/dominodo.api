using Dominodo.Users.Domain.Memberships;
using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dominodo.Users.Persistence.Configurations;

internal sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.RoleId).IsRequired();
        builder.Property(m => m.Status).HasConversion<int>().IsRequired();
        builder.Property(m => m.InvitedAtUtc);
        builder.Property(m => m.JoinedAtUtc);

        // One role per person per conjunto.
        builder.HasIndex(m => new { m.UserId, m.TenantId }).IsUnique();
        builder.HasIndex(m => m.TenantId);

        // Internal FKs (same module). No FK on TenantId — Tenant lives in another module (rule #2).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(m => m.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit timestamps via the interceptor (shadow props, as on User).
        builder.Property<DateTimeOffset>("CreatedAtUtc");
        builder.Property<DateTimeOffset>("UpdatedAtUtc");

        builder.Ignore(m => m.DomainEvents);
    }
}
