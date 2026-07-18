using System.Diagnostics;
using System.Net.Sockets;
using Dominodo.Admin.Persistence;
using Dominodo.Tenants.Persistence;
using Dominodo.Users.Persistence;

namespace Dominodo.Api;

// Development-only startup convenience: make sure the local SQL Server container is up, then apply each
// module's pending migrations — so pressing F5 in the IDE gives you a ready database with no manual steps.
// Everything here is idempotent ("only if not already done"):
//   • Docker is invoked ONLY when the DB port isn't already reachable.
//   • MigrateAsync applies ONLY migrations not yet recorded in __ef_migrations.
// Never runs outside Development. Opt out with "DevBootstrap:Enabled": false.
internal static class DevBootstrap
{
    public static async Task EnsureLocalDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("DevBootstrap:Enabled", false))
        {
            return;
        }

        var logger = app.Logger;
        var connectionString = app.Configuration.GetConnectionString("Dominodo");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("DevBootstrap: no 'Dominodo' connection string configured; skipping");
            return;
        }

        if (!await IsReachableAsync(connectionString))
        {
            StartDockerCompose(logger);
        }

        // Apply each module's migrations. A module only migrates its own schema (separate __ef_migrations).
        // Best-effort: a failure here (e.g. the model drifted ahead of its last migration, or the DB is
        // unreachable) must NOT block booting — log actionable guidance and let the app start.
        await MigrateAsync(logger, "Users", app.Services.MigrateUsersDatabaseAsync);
        await MigrateAsync(logger, "Admin", app.Services.MigrateAdminDatabaseAsync);
        await MigrateAsync(logger, "Tenants", app.Services.MigrateTenantsDatabaseAsync);
    }

    private static async Task MigrateAsync(ILogger logger, string module, Func<CancellationToken, Task> migrate)
    {
        try
        {
            await migrate(CancellationToken.None);
            logger.LogInformation("DevBootstrap: {Module} migrations applied", module);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "DevBootstrap: could not migrate {Module}. If the model changed, add a migration " +
                "(dotnet ef migrations add <Name> …) then run ./scripts/db.sh. Continuing startup against the existing schema",
                module);
        }
    }

    // Quick TCP probe to the DB host:port. Reachable → the container is already running, skip Docker.
    private static async Task<bool> IsReachableAsync(string connectionString)
    {
        var (host, port) = ParseServer(connectionString);
        try
        {
            using var client = new TcpClient();
            var connect = client.ConnectAsync(host, port);
            var completed = await Task.WhenAny(connect, Task.Delay(TimeSpan.FromSeconds(2)));
            return completed == connect && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    // Parse "Server=localhost,1435" / "Data Source=tcp:host,port" → (host, port); default port 1433.
    private static (string Host, int Port) ParseServer(string connectionString)
    {
        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = segment.Split('=', 2);
            if (kv.Length != 2)
            {
                continue;
            }

            var key = kv[0].Trim();
            if (key.Equals("Server", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Data Source", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Address", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Addr", StringComparison.OrdinalIgnoreCase))
            {
                var value = kv[1].Trim().Replace("tcp:", string.Empty, StringComparison.OrdinalIgnoreCase);
                var parts = value.Split(',', 2);
                var host = parts[0].Trim();
                var port = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var p) ? p : 1433;
                return (host, port);
            }
        }

        return ("localhost", 1433);
    }

    private static void StartDockerCompose(ILogger logger)
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            logger.LogWarning("DevBootstrap: docker-compose.yml not found; start the database yourself.");
            return;
        }

        logger.LogInformation("DevBootstrap: database not reachable — running 'docker compose up -d --wait'…");
        try
        {
            using var process = Process.Start(new ProcessStartInfo("docker", "compose up -d --wait")
            {
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            if (process is null)
            {
                logger.LogWarning("DevBootstrap: could not launch Docker; start the database yourself.");
                return;
            }

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                logger.LogWarning(
                    "DevBootstrap: 'docker compose up' exited with {Code}: {Error}",
                    process.ExitCode,
                    process.StandardError.ReadToEnd());
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DevBootstrap: Docker not available; start the database yourself.");
        }
    }

    // Walk up from the running binary until we find the repo root (the folder holding docker-compose.yml).
    private static string? FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}
