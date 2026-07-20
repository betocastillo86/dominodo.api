using Dominodo.Shared.Infrastructure.Persistence;
using Dominodo.Shared.Kernel;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Tenants.Domain.Tenants;
using Dominodo.Tenants.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence.Durability;
using Wolverine.SqlServer;

namespace Dominodo.Tenants.Persistence;

public static class DependencyInjection
{
    // Registers the module's repositories + IUnitOfWork. The TenantsDbContext itself is registered by
    // AddTenantsMessaging (below) through Wolverine's EF integration, so the module's outbox is enrolled.
    public static IServiceCollection AddTenantsPersistence(this IServiceCollection services)
    {
        // The unit of work routes SaveChanges through Wolverine's durable outbox (persists the
        // aggregate changes + domain-event envelopes in one transaction, then flushes async).
        services.AddScoped<IUnitOfWork>(sp =>
            new WolverineUnitOfWork<TenantsDbContext>(sp.GetRequiredService<IDbContextOutbox<TenantsDbContext>>()));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IApartmentRepository, ApartmentRepository>();

        return services;
    }

    // Enrolls this module's DbContext with Wolverine (EF transactional middleware + durable outbox) and
    // an ancillary SQL Server message store keyed to the module schema. Called by the host inside
    // UseWolverine; keeps TenantsDbContext internal to the module (doc 07).
    public static void AddTenantsMessaging(this WolverineOptions opts, string connectionString)
    {
        opts.Services.AddDbContextWithWolverineIntegration<TenantsDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__ef_migrations", TenantsDbContext.Schema));

            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        opts.PersistMessagesWithSqlServer(connectionString, role: MessageStoreRole.Ancillary)
            .Enroll<TenantsDbContext>();
    }

    // Applies this module's pending EF migrations. Public entry point so the host can migrate the module
    // (dev convenience) without TenantsDbContext leaving the assembly — it stays internal. Idempotent:
    // MigrateAsync only runs migrations not yet recorded in __ef_migrations.
    public static async Task MigrateTenantsDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantsDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    // IntegrationTests-only: seeds the fixed Active tenant that the Users module's Tenant-scope permission
    // fixtures hold memberships in, so X-Tenant: integration-test resolves it. Id + slug MUST match
    // Users' IntegrationTestSeedData.IntegrationTenantId / IntegrationTenantSlug. Idempotent.
    // Writes directly with SaveChangesAsync (reference data — no outbox/domain-event dispatch).
    public static async Task SeedIntegrationTestTenantAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        var tenantId = Guid.Parse("00000000-0000-0000-0000-0000000000E2");
        const string slug = "integration-test";

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantsDbContext>();

        if (await db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
        {
            return;
        }

        db.Tenants.Add(Tenant.CreateSeed(
            tenantId,
            slug,
            name: "Integration Test",
            type: TenantType.Conjunto,
            address: "Integration Ave 1",
            city: "Bogotá",
            country: "CO"));

        await db.SaveChangesAsync(ct);
    }
}
