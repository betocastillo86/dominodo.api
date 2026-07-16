using Dominodo.Admin.Domain.Ports;
using Dominodo.Admin.Persistence.Repositories;
using Dominodo.Shared.Infrastructure.Persistence;
using Dominodo.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence.Durability;
using Wolverine.SqlServer;

namespace Dominodo.Admin.Persistence;

public static class DependencyInjection
{
    // Registers the module's repositories + IUnitOfWork. The AdminDbContext itself is registered by
    // AddAdminMessaging (below) through Wolverine's EF integration, so the module's outbox is enrolled.
    public static IServiceCollection AddAdminPersistence(this IServiceCollection services)
    {
        // The unit of work routes SaveChanges through Wolverine's durable outbox (persists the
        // aggregate changes + domain-event envelopes in one transaction, then flushes async).
        services.AddScoped<IUnitOfWork>(sp =>
            new WolverineUnitOfWork<AdminDbContext>(sp.GetRequiredService<IDbContextOutbox<AdminDbContext>>()));
        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();

        return services;
    }

    // Enrolls this module's DbContext with Wolverine (EF transactional middleware + durable outbox) and
    // an ancillary SQL Server message store keyed to the module schema. Called by the host inside
    // UseWolverine; keeps AdminDbContext internal to the module (doc 07).
    public static void AddAdminMessaging(this WolverineOptions opts, string connectionString)
    {
        opts.Services.AddDbContextWithWolverineIntegration<AdminDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__ef_migrations", AdminDbContext.Schema));

            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        opts.PersistMessagesWithSqlServer(connectionString, role: MessageStoreRole.Ancillary)
            .Enroll<AdminDbContext>();
    }
}
