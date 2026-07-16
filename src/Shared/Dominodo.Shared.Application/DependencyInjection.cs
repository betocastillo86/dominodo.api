using Dominodo.Shared.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dominodo.Shared.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedApplication(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        return services;
    }
}
