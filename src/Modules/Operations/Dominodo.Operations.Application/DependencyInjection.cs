using System.Reflection;
using Dominodo.Operations.Application.IntegrationBridges;
using Dominodo.Operations.Application.ModuleApi;
using Dominodo.Operations.Contracts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Configuration;

namespace Dominodo.Operations.Application;

public static class DependencyInjection
{
    public static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;

    public static IServiceCollection AddOperationsModule(
        this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ApplicationAssembly));
        services.AddValidatorsFromAssembly(ApplicationAssembly, includeInternalTypes: true);

        services.AddScoped<IOperationsModuleApi, OperationsModuleApi>();

        return services;
    }

    public static void AddOperationsHandlers(this HandlerDiscovery discovery)
    {
        // Domain→integration bridges: each translates a domain event into its integration event.
        discovery.IncludeType<WhenRequestOpened_PublishIntegrationEvent>();
        discovery.IncludeType<WhenRequestUpdated_PublishIntegrationEvent>();
        discovery.IncludeType<WhenRequestStatusChanged_PublishIntegrationEvent>();
        discovery.IncludeType<WhenRequestClosed_PublishIntegrationEvent>();
        discovery.IncludeType<WhenDeliveryRegistered_PublishIntegrationEvent>();
        discovery.IncludeType<WhenDeliveryDelivered_PublishIntegrationEvent>();
        discovery.IncludeType<WhenVisitRegistered_PublishIntegrationEvent>();
        discovery.IncludeType<WhenAnnouncementPublished_PublishIntegrationEvent>();
    }
}
