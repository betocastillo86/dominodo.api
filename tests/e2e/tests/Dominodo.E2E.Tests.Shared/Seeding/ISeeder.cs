namespace Dominodo.E2E.Tests.Shared.Seeding;

/// <summary>
/// Seam for future DB/data seeding. No-op today: registration is anonymous and super-admin access
/// is achieved by minting a JWT (not a DB seed). Later modules (e.g. Tenants) implement this.
/// </summary>
public interface ISeeder
{
    Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
