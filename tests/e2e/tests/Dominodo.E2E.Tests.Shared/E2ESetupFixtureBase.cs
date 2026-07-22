using Dominodo.E2E.Clients;
using Dominodo.E2E.Clients.Core.Api;
using Dominodo.E2E.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;

namespace Dominodo.E2E.Tests.Shared;

/// <summary>
/// Per-assembly bootstrap. Each module test project has a one-line <c>[SetUpFixture]</c> deriving from
/// this: it builds the DI container (config + Serilog + Refit clients + handlers + JWT factory), waits
/// for the API to be healthy, then runs the (currently no-op) seeding seam.
/// </summary>
public abstract class E2ESetupFixtureBase
{
    public static ServiceProvider ServiceProvider { get; private set; } = default!;

    [OneTimeSetUp]
    public async Task BaseOneTimeSetUp()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(TestContext.CurrentContext.TestDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        var services = new ServiceCollection();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });

        services.AddSingleton<IConfiguration>(configuration);

        var apiSettings = configuration.GetSection(ApiSettings.SectionName).Get<ApiSettings>()
            ?? throw new ArgumentException($"'{ApiSettings.SectionName}' section not found in configuration.");
        services.AddSingleton(apiSettings);

        services.AddUsersClient();
        services.AddTenantsClient();
        services.AddAdminClient();
        services.AddSqlClient();
        services.AddCoreServices(configuration);

        ServiceProvider = services.BuildServiceProvider();

        await WaitForApiHealthyAsync(apiSettings.BaseUrl);

        await SeedAsync(ServiceProvider);
    }

    [OneTimeTearDown]
    public void BaseOneTimeTearDown()
    {
        ServiceProvider?.Dispose();
        Log.CloseAndFlush();
    }

    /// <summary>Seeding seam. No-op by default; later modules override to seed their data.</summary>
    protected virtual Task SeedAsync(IServiceProvider serviceProvider)
    {
        return Task.CompletedTask;
    }

    private static async Task WaitForApiHealthyAsync(string baseUrl)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

        var timeout = TimeSpan.FromSeconds(60);
        var pollInterval = TimeSpan.FromSeconds(1);
        var deadline = DateTime.UtcNow.Add(timeout);
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await httpClient.GetAsync("/health/ready");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                lastError = new InvalidOperationException(
                    $"Health check returned {(int)response.StatusCode}.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastError = ex;
            }

            await Task.Delay(pollInterval);
        }

        throw new InvalidOperationException(
            $"API at '{baseUrl}' did not become healthy at /health/ready within {timeout.TotalSeconds:0}s. " +
            "Ensure the API and SQL Server are running (see docker-compose + `dotnet run`).",
            lastError);
    }
}
