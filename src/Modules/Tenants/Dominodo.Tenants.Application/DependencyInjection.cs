using System.Reflection;
using Dominodo.Shared.Abstractions;
using Dominodo.Tenants.Application.IntegrationBridges;
using Dominodo.Tenants.Application.ModuleApi;
using Dominodo.Tenants.Contracts;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Configuration;

namespace Dominodo.Tenants.Application;

public static class DependencyInjection
{
    public static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;

    // Registers the module's application layer (MediatR handlers, validators, ports, public facade impl).
    // Persistence is wired by the host via AddTenantsPersistence — Application never references it,
    // honoring the inward dependency rule (README dependency table).
    public static IServiceCollection AddTenantsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ApplicationAssembly));
        services.AddValidatorsFromAssembly(ApplicationAssembly, includeInternalTypes: true);

        // The real slug → TenantId directory, overriding Shared.Infrastructure's NullTenantDirectory
        // fallback. Singleton (it out-lives the request; it reaches the scoped repository via a scope
        // factory) with an in-memory slug cache — this is what makes X-Tenant resolvable (doc 09).
        services.AddMemoryCache();
        services.AddSingleton<ITenantDirectory, TenantDirectory>();

        services.AddScoped<ITenantsModuleApi, TenantsModuleApi>();

        return services;
    }

    // Registers this module's Wolverine message handlers explicitly (conventional discovery skips
    // handlers the host doesn't scan). Called by the host inside UseWolverine. These are the
    // domain→integration bridges and the directory-cache invalidator.
    public static void AddTenantsHandlers(this HandlerDiscovery discovery)
    {
        discovery.IncludeType<WhenTenantCreated_PublishIntegrationEvent>();
        discovery.IncludeType<WhenApartmentCreated_PublishIntegrationEvent>();
        discovery.IncludeType<WhenResidentAssigned_PublishIntegrationEvent>();
        discovery.IncludeType<WhenResidentRemoved_PublishIntegrationEvent>();
        discovery.IncludeType<WhenTenantStatusChanged_InvalidateDirectoryCache>();
    }
}
