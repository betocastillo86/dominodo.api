using System.Net.Http.Headers;
using System.Net.Mime;
using Dominodo.E2E.Clients.Core.Api;
using Dominodo.E2E.Clients.Core.Handlers;
using Dominodo.E2E.Clients.Dev;
using Dominodo.E2E.Clients.Modules.Admin;
using Dominodo.E2E.Clients.Modules.Operations;
using Dominodo.E2E.Clients.Modules.Tenants;
using Dominodo.E2E.Clients.Modules.Users;
using Dominodo.E2E.Core.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refit;

namespace Dominodo.E2E.Clients;

public static class ClientsServiceRegister
{
    public static IServiceCollection AddUsersClient(this IServiceCollection services)
    {
        services.AddTransient<Modules.Users.UsersRequestBuilder>();

        services.AddRefitClient<IUsersClient>(GetDefaultRefitSettings())
            .ConfigureHttpClient(DefaultConfigurationClient)
            .WithTenantHeaderHandler()
            .WithAuthorizationHandler()
            .WithCorrelationIdHandler()
            .WithLoggingHandler()
            .WithDefaultRetryHandler();

        return services;
    }

    public static IServiceCollection AddAdminClient(this IServiceCollection services)
    {
        services.AddTransient<Modules.Admin.AdminRequestBuilder>();

        services.AddRefitClient<IAdminClient>(GetDefaultRefitSettings())
            .ConfigureHttpClient(DefaultConfigurationClient)
            .WithTenantHeaderHandler()
            .WithAuthorizationHandler()
            .WithCorrelationIdHandler()
            .WithLoggingHandler()
            .WithDefaultRetryHandler();

        return services;
    }

    public static IServiceCollection AddTenantsClient(this IServiceCollection services)
    {
        services.AddTransient<Modules.Tenants.TenantsRequestBuilder>();

        services.AddRefitClient<ITenantsClient>(GetDefaultRefitSettings())
            .ConfigureHttpClient(DefaultConfigurationClient)
            .WithTenantHeaderHandler()
            .WithAuthorizationHandler()
            .WithCorrelationIdHandler()
            .WithLoggingHandler()
            .WithDefaultRetryHandler();

        return services;
    }

    public static IServiceCollection AddOperationsClient(this IServiceCollection services)
    {
        services.AddTransient<Modules.Operations.OperationsRequestBuilder>();

        services.AddRefitClient<IOperationsClient>(GetDefaultRefitSettings())
            .ConfigureHttpClient(DefaultConfigurationClient)
            .WithTenantHeaderHandler()
            .WithAuthorizationHandler()
            .WithCorrelationIdHandler()
            .WithLoggingHandler()
            .WithDefaultRetryHandler();

        return services;
    }

    /// <summary>
    /// Registers the dev-only raw-SQL client (<see cref="ISqlClient"/>). Used purely for E2E Arrange —
    /// the underlying endpoint returns 404 outside Development.
    /// </summary>
    public static IServiceCollection AddSqlClient(this IServiceCollection services)
    {
        services.AddRefitClient<ISqlClient>(GetDefaultRefitSettings())
            .ConfigureHttpClient(DefaultConfigurationClient)
            .WithTenantHeaderHandler()
            .WithCorrelationIdHandler()
            .WithLoggingHandler()
            .WithDefaultRetryHandler();

        return services;
    }

    private static RefitSettings GetDefaultRefitSettings()
    {
        // System.Text.Json aligned to the Dominodo host (camelCase, enums-as-strings, ISO-8601).
        return new RefitSettings(new SystemTextJsonContentSerializer(E2EJsonOptions.Default));
    }

    // Handler chain — order mirrors the Pollaya E2E suite: tenant → auth → correlation → logging → retry.
    // First added = outermost, so tenant/correlation headers are present by the time LoggingHandler logs.
    private static IHttpClientBuilder WithTenantHeaderHandler(this IHttpClientBuilder builder)
    {
        builder.Services.TryAddTransient<TenantHeaderHandler>();
        builder.AddHttpMessageHandler<TenantHeaderHandler>();
        return builder;
    }

    private static IHttpClientBuilder WithAuthorizationHandler(this IHttpClientBuilder builder)
    {
        builder.Services.TryAddTransient<AuthorizationHandler>();
        builder.AddHttpMessageHandler<AuthorizationHandler>();
        return builder;
    }

    private static IHttpClientBuilder WithCorrelationIdHandler(this IHttpClientBuilder builder)
    {
        builder.Services.TryAddTransient<CorrelationIdHandler>();
        builder.AddHttpMessageHandler<CorrelationIdHandler>();
        return builder;
    }

    private static IHttpClientBuilder WithLoggingHandler(this IHttpClientBuilder builder)
    {
        builder.Services.TryAddTransient<LoggingHandler>();
        builder.AddHttpMessageHandler<LoggingHandler>();
        return builder;
    }

    private static IHttpClientBuilder WithDefaultRetryHandler(this IHttpClientBuilder builder)
    {
        builder.Services.TryAddTransient<DefaultRetryHandler>();
        builder.AddHttpMessageHandler<DefaultRetryHandler>();
        return builder;
    }

    private static void DefaultConfigurationClient(IServiceProvider sp, HttpClient httpClient)
    {
        var apiSettings = sp.GetRequiredService<ApiSettings>();

        if (string.IsNullOrWhiteSpace(apiSettings.BaseUrl))
        {
            throw new ArgumentNullException(nameof(apiSettings.BaseUrl));
        }

        httpClient.BaseAddress = new Uri(apiSettings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds);
        httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
    }
}
