using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dominodo.Operations.Persistence;

// Design-time factory so `dotnet ef` can build OperationsDbContext without booting the whole host.
internal sealed class OperationsDbContextFactory : IDesignTimeDbContextFactory<OperationsDbContext>
{
    public OperationsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Dominodo")
            ?? "Server=localhost,1435;Database=Dominodo;User Id=sa;Password=Dominodo!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=true";

        var options = new DbContextOptionsBuilder<OperationsDbContext>()
            .UseSqlServer(connectionString,
                sql => sql.MigrationsHistoryTable("__ef_migrations", OperationsDbContext.Schema))
            .Options;

        return new OperationsDbContext(options);
    }
}
