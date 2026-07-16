using Dominodo.Shared.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Dominodo.Adapters.WhatsApp;

public static class DependencyInjection
{
    public static IServiceCollection AddWhatsAppAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<WhatsAppSenderOptions>()
            .Bind(configuration.GetSection(WhatsAppSenderOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "Adapters:WhatsApp:BaseUrl is required.")
            .ValidateOnStart();

        services.AddHttpClient<IWhatsAppSender, WhatsAppSender>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WhatsAppSenderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            }
        })
        .AddResilienceHandler("whatsapp", builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            });
            builder.AddTimeout(TimeSpan.FromSeconds(5));
            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions());
        });

        return services;
    }
}
