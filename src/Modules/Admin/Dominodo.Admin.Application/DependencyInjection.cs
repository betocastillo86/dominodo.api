using System.Reflection;
using Dominodo.Admin.Application.Consumers;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Configuration;

namespace Dominodo.Admin.Application;

public static class DependencyInjection
{
    public static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;

    // Registers the module's application layer (MediatR handlers, validators). Persistence is wired by
    // the host via AddAdminPersistence; the Wolverine integration-event handler via AddAdminHandlers.
    public static IServiceCollection AddAdminModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ApplicationAssembly));
        services.AddValidatorsFromAssembly(ApplicationAssembly, includeInternalTypes: true);

        return services;
    }

    // Registers this module's Wolverine message handlers explicitly, so the module owns exactly which
    // handlers the bus exposes (rather than relying on assembly-wide conventional scanning). Called by
    // the host inside UseWolverine.
    public static void AddAdminHandlers(this HandlerDiscovery discovery)
    {
        discovery.IncludeType<UserOtpRequestedHandler>();
    }
}
