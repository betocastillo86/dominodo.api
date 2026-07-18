using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dominodo.Tenants.Persistence;

// Design-time factory so `dotnet ef` can build TenantsDbContext without booting the whole host.
internal sealed class TenantsDbContextFactory : IDesignTimeDbContextFactory<TenantsDbContext>
{
    public TenantsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Dominodo")
            ?? "Server=localhost,1435;Database=Dominodo;User Id=sa;Password=Dominodo!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=true";

        var options = new DbContextOptionsBuilder<TenantsDbContext>()
            .UseSqlServer(connectionString,
                sql => sql.MigrationsHistoryTable("__ef_migrations", TenantsDbContext.Schema))
            .Options;

        return new TenantsDbContext(options);
    }
}
