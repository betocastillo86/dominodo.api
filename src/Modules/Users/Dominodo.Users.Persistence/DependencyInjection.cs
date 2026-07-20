using Dominodo.Shared.Kernel;
using Dominodo.Shared.Infrastructure.Persistence;
using Dominodo.Users.Domain.Ports;
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

    // IntegrationTests-only: seeds a Platform role + user + assignment per permission (fixed ids) so tests can
    // authenticate as a user carrying exactly one permission. Public entry point so the host can invoke it
    // without UsersDbContext leaving the assembly. Idempotent: each row is added only if its id is absent.
    // Writes directly with SaveChangesAsync (not the Wolverine unit of work) — this is reference data, so no
    // outbox/domain-event dispatch should occur.
    public static async Task SeedIntegrationTestDataAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        foreach (var fixture in IntegrationTestSeedData.Fixtures)
        {
            if (!await db.Roles.AnyAsync(r => r.Id == fixture.RoleId, ct))
            {
                db.Roles.Add(IntegrationTestSeedData.BuildRole(fixture));
            }

            if (!await db.Users.AnyAsync(u => u.Id == fixture.UserId, ct))
            {
                db.Users.Add(IntegrationTestSeedData.BuildUser(fixture));
            }

            if (!await db.PlatformRoleAssignments.AnyAsync(a => a.Id == fixture.AssignmentId, ct))
            {
                db.PlatformRoleAssignments.Add(IntegrationTestSeedData.BuildAssignment(fixture));
            }
        }

        // "Rol Public": a permission-less role + user, so tests can assert 403 for a user with no permissions.
        if (!await db.Roles.AnyAsync(r => r.Id == IntegrationTestSeedData.PublicRoleId, ct))
        {
            db.Roles.Add(IntegrationTestSeedData.BuildPublicRole());
        }

        if (!await db.Users.AnyAsync(u => u.Id == IntegrationTestSeedData.PublicUserId, ct))
        {
            db.Users.Add(IntegrationTestSeedData.BuildPublicUser());
        }

        if (!await db.PlatformRoleAssignments.AnyAsync(a => a.Id == IntegrationTestSeedData.PublicAssignmentId, ct))
        {
            db.PlatformRoleAssignments.Add(IntegrationTestSeedData.BuildPublicAssignment());
        }

        await db.SaveChangesAsync(ct);
    }
}
