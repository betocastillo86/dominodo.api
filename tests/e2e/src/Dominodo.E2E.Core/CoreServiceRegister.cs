using Dominodo.E2E.Core.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dominodo.E2E.Core;

public static class CoreServiceRegister
{
    public static IServiceCollection AddCoreServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddJwtTokenFactory(configuration);
        return services;
    }

    public static IServiceCollection AddJwtTokenFactory(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new ArgumentException($"'{JwtSettings.SectionName}' section not found in configuration.");

        services.AddSingleton(jwtSettings);
        services.AddSingleton<JwtTokenFactory>();
        return services;
    }
}
