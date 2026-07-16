using Dominodo.Shared.Kernel;
using Dominodo.Shared.Infrastructure.Persistence;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Persistence.Repositories;
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
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UsersDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPlatformRoleAssignmentRepository, PlatformRoleAssignmentRepository>();

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
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<DispatchDomainEventsInterceptor>());
        });

        opts.PersistMessagesWithSqlServer(connectionString, role: MessageStoreRole.Ancillary)
            .Enroll<UsersDbContext>();
    }
}
