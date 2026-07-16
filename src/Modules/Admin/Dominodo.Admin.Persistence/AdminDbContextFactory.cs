using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dominodo.Admin.Persistence;

// Design-time factory so `dotnet ef` can build AdminDbContext without booting the whole host.
internal sealed class AdminDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Dominodo")
            ?? "Server=localhost,1435;Database=Dominodo;User Id=sa;Password=Dominodo!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=true";

        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseSqlServer(connectionString,
                sql => sql.MigrationsHistoryTable("__ef_migrations", AdminDbContext.Schema))
            .Options;

        return new AdminDbContext(options);
    }
}
