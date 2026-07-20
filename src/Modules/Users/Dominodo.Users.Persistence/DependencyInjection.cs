using Dominodo.Shared.Kernel;
using Dominodo.Shared.Infrastructure.Persistence;
using Dominodo.Users.Domain.Memberships;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Roles;
using Dominodo.Users.Domain.Users;
using Dominodo.Users.Persistence.Repositories;
using Dominodo.Users.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence.Durability;
using Wolverine.SqlServer;

namespace Dominodo.Users.Persistence;

public static class DependencyInjection
{
    // Registers the module's repositories + IUnitOfWork. The UsersDbContext itself is registered by
    // AddUsersMessaging (below) through Wolverine's EF integration, so the module's outbox is enrolled.
    public static IServiceCollection AddUsersPersistence(this IServiceCollection services)
    {
        // The unit of work routes SaveChanges through Wolverine's durable outbox (persists the
        // aggregate changes + domain-event envelopes in one transaction, then flushes async).
        services.AddScoped<IUnitOfWork>(sp =>
            new WolverineUnitOfWork<UsersDbContext>(sp.GetRequiredService<IDbContextOutbox<UsersDbContext>>()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPlatformRoleAssignmentRepository, PlatformRoleAssignmentRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();

        return services;
    }

    // Enrolls this module's DbContext with Wolverine (EF transactional middleware + durable outbox) and
    // an ancillary SQL Server message store keyed to the module schema. Called by the host inside
    // UseWolverine; keeps UsersDbContext internal to the module (doc 07).
    public static void AddUsersMessaging(this WolverineOptions opts, string connectionString)
    {
        opts.Services.AddDbContextWithWolverineIntegration<UsersDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__ef_migrations", UsersDbContext.Schema));

            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        opts.PersistMessagesWithSqlServer(connectionString, role: MessageStoreRole.Ancillary)
            .Enroll<UsersDbContext>();
    }

    // Applies this module's pending EF migrations. Public entry point so the host can migrate the module
    // (dev convenience) without UsersDbContext leaving the assembly — it stays internal. Idempotent:
    // MigrateAsync only runs migrations not yet recorded in __ef_migrations.
    public static async Task MigrateUsersDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    // IntegrationTests-only: per permission seeds a Platform role + user + assignment AND a Tenant role +
    // user + Active membership (fixed ids) so tests can authenticate as a user carrying exactly one
    // permission via either the platform or the tenant branch. Public entry point so the host can invoke it
    // without UsersDbContext leaving the assembly. Writes directly with SaveChangesAsync (not the Wolverine
    // unit of work) — this is reference data, so no outbox/domain-event dispatch should occur.
    //
    // RECONCILING (not merely add-if-absent): fixture role ids live in a reserved negative range, unreachable
    // by dynamic `MAX(Id)+1` role creation. On the shared dev DB, legacy fixtures used the old positive scheme
    // (1000+/2000+), which collided with test-created roles. So per fixture we (1) drop any legacy role holding
    // the fixture's unique Name at a different id — freeing the Name and the collided positive id — and (2)
    // repoint the fixture user's assignment/membership at the negative role, dropping stale ones. This heals
    // the polluted dev DB and is a harmless no-op on a fresh DB.
    public static async Task SeedIntegrationTestDataAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        // Phase 1 — cleanup: drop legacy roles squatting a fixture Name at a non-reserved id, plus stale
        // assignments/memberships for fixture users pointing at the wrong role. Committed on its own so the
        // reserved-range inserts in Phase 2 don't race the unique Name index (INSERT-before-DELETE hazard).
        foreach (var fixture in IntegrationTestSeedData.Fixtures)
        {
            await RemoveLegacyRoleByNameAsync(db, fixture.RoleId, fixture.RoleName, ct);
            await RemoveStalePlatformAssignmentsAsync(db, fixture.UserId, fixture.RoleId, ct);
        }

        foreach (var fixture in IntegrationTestSeedData.TenantFixtures)
        {
            await RemoveLegacyRoleByNameAsync(db, fixture.RoleId, fixture.RoleName, ct);
            await RemoveStaleMembershipsAsync(db, fixture.UserId, IntegrationTestSeedData.IntegrationTenantId, fixture.RoleId, ct);
        }

        await RemoveLegacyRoleByNameAsync(db, IntegrationTestSeedData.PublicRoleId, IntegrationTestSeedData.PublicRoleName, ct);
        await RemoveStalePlatformAssignmentsAsync(db, IntegrationTestSeedData.PublicUserId, IntegrationTestSeedData.PublicRoleId, ct);

        await db.SaveChangesAsync(ct);

        // Phase 2 — ensure each fixture (role + user + assignment/membership) exists at its reserved id.
        foreach (var fixture in IntegrationTestSeedData.Fixtures)
        {
            await EnsureRoleAsync(db, fixture.RoleId, () => IntegrationTestSeedData.BuildRole(fixture), ct);
            await EnsureUserAsync(db, fixture.UserId, () => IntegrationTestSeedData.BuildUser(fixture), ct);
            await EnsurePlatformAssignmentAsync(db, fixture.UserId, fixture.RoleId, () => IntegrationTestSeedData.BuildAssignment(fixture), ct);
        }

        // Tenant-scope counterpart: a Tenant role + user + Active membership per permission, so tests can
        // exercise the tenant branch of permission resolution (token + X-Tenant header).
        foreach (var fixture in IntegrationTestSeedData.TenantFixtures)
        {
            await EnsureRoleAsync(db, fixture.RoleId, () => IntegrationTestSeedData.BuildTenantRole(fixture), ct);
            await EnsureUserAsync(db, fixture.UserId, () => IntegrationTestSeedData.BuildTenantUser(fixture), ct);
            await EnsureMembershipAsync(db, fixture.UserId, IntegrationTestSeedData.IntegrationTenantId, fixture.RoleId, () => IntegrationTestSeedData.BuildMembership(fixture), ct);
        }

        // "Rol Public": a permission-less role + user, so tests can assert 403 for a user with no permissions.
        await EnsureRoleAsync(db, IntegrationTestSeedData.PublicRoleId, IntegrationTestSeedData.BuildPublicRole, ct);
        await EnsureUserAsync(db, IntegrationTestSeedData.PublicUserId, IntegrationTestSeedData.BuildPublicUser, ct);
        await EnsurePlatformAssignmentAsync(db, IntegrationTestSeedData.PublicUserId, IntegrationTestSeedData.PublicRoleId, IntegrationTestSeedData.BuildPublicAssignment, ct);

        await db.SaveChangesAsync(ct);
    }

    // Cleanup: remove a legacy role holding the fixture's unique Name at a different id (cascades its
    // RolePermissions), freeing both the Name and the collided positive id for the reserved-range insert.
    private static async Task RemoveLegacyRoleByNameAsync(UsersDbContext db, int roleId, string roleName, CancellationToken ct)
    {
        var legacy = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.Id != roleId, ct);
        if (legacy is not null)
        {
            db.Roles.Remove(legacy);
        }
    }

    private static async Task RemoveStalePlatformAssignmentsAsync(UsersDbContext db, Guid userId, int roleId, CancellationToken ct)
    {
        var stale = await db.PlatformRoleAssignments.Where(a => a.UserId == userId && a.RoleId != roleId).ToListAsync(ct);
        if (stale.Count > 0)
        {
            db.PlatformRoleAssignments.RemoveRange(stale);
        }
    }

    private static async Task RemoveStaleMembershipsAsync(UsersDbContext db, Guid userId, Guid tenantId, int roleId, CancellationToken ct)
    {
        var stale = await db.Memberships.Where(m => m.UserId == userId && m.TenantId == tenantId && m.RoleId != roleId).ToListAsync(ct);
        if (stale.Count > 0)
        {
            db.Memberships.RemoveRange(stale);
        }
    }

    private static async Task EnsureRoleAsync(UsersDbContext db, int roleId, Func<Role> build, CancellationToken ct)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct))
        {
            db.Roles.Add(build());
        }
    }

    private static async Task EnsureUserAsync(UsersDbContext db, Guid userId, Func<User> build, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            db.Users.Add(build());
        }
    }

    private static async Task EnsurePlatformAssignmentAsync(UsersDbContext db, Guid userId, int roleId, Func<PlatformRoleAssignment> build, CancellationToken ct)
    {
        if (!await db.PlatformRoleAssignments.AnyAsync(a => a.UserId == userId && a.RoleId == roleId, ct))
        {
            db.PlatformRoleAssignments.Add(build());
        }
    }

    private static async Task EnsureMembershipAsync(UsersDbContext db, Guid userId, Guid tenantId, int roleId, Func<Membership> build, CancellationToken ct)
    {
        if (!await db.Memberships.AnyAsync(m => m.UserId == userId && m.TenantId == tenantId && m.RoleId == roleId, ct))
        {
            db.Memberships.Add(build());
        }
    }
}
