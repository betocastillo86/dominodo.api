using Dominodo.Operations.Application.Abstractions;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Persistence.Repositories;
using Dominodo.Operations.Persistence.Sequences;
using Dominodo.Shared.Infrastructure.Persistence;
using Dominodo.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence.Durability;
using Wolverine.SqlServer;

namespace Dominodo.Operations.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddOperationsPersistence(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork>(sp =>
            new WolverineUnitOfWork<OperationsDbContext>(sp.GetRequiredService<IDbContextOutbox<OperationsDbContext>>()));

        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<ISequenceProvider, SequenceProvider>();

        return services;
    }

    public static void AddOperationsMessaging(this WolverineOptions opts, string connectionString)
    {
        opts.Services.AddDbContextWithWolverineIntegration<OperationsDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__ef_migrations", OperationsDbContext.Schema));

            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        opts.PersistMessagesWithSqlServer(connectionString, role: MessageStoreRole.Ancillary)
            .Enroll<OperationsDbContext>();
    }

    public static async Task MigrateOperationsDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
