using Dominodo.Shared.Kernel;
using Dominodo.Users.Domain.Authentication;
using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Users.Persistence;

internal sealed class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public const string Schema = "users";

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<PlatformRoleAssignment> PlatformRoleAssignments => Set<PlatformRoleAssignment>();
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);

        // Wolverine's durable message storage (envelope tables) is provisioned by the bus in the
        // "wolverine" schema, not by this module's EF migrations (doc 07).
    }
}
