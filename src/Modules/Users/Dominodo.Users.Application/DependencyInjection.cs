using System.Reflection;
using Dominodo.Users.Application.Abstractions;
using Dominodo.Users.Application.IntegrationBridges;
using Dominodo.Users.Application.ModuleApi;
using Dominodo.Users.Application.Security;
using Dominodo.Users.Contracts;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Configuration;

namespace Dominodo.Users.Application;

public static class DependencyInjection
{
    public static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;

    // Registers the module's application layer (MediatR handlers, validators, ports, public facade impl).
    // Persistence is wired by the host via AddUsersPersistence — Application never references it,
    // honoring the inward dependency rule (README dependency table).
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ApplicationAssembly));
        services.AddValidatorsFromAssembly(ApplicationAssembly, includeInternalTypes: true);

        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));

        services.AddScoped<IUsersModuleApi, UsersModuleApi>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }

    // Registers this module's Wolverine message handlers explicitly (conventional discovery skips
    // handlers the host doesn't scan). Called by the host inside UseWolverine. These are the
    // membership domain→integration bridges.
    public static void AddUsersHandlers(this HandlerDiscovery discovery)
    {
        discovery.IncludeType<WhenMembershipCreated_PublishIntegrationEvent>();
        discovery.IncludeType<WhenMembershipSuspended_PublishIntegrationEvent>();
        discovery.IncludeType<WhenMembershipRoleChanged_PublishIntegrationEvent>();
    }
}
