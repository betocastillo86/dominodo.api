using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dominodo.Users.Persistence;

// Design-time factory so `dotnet ef` can build UsersDbContext without booting the whole host.
internal sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Dominodo")
            ?? "Server=localhost,1435;Database=Dominodo;User Id=sa;Password=Dominodo!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=true";

        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseSqlServer(connectionString,
                sql => sql.MigrationsHistoryTable("__ef_migrations", UsersDbContext.Schema))
            .Options;

        return new UsersDbContext(options);
    }
}
