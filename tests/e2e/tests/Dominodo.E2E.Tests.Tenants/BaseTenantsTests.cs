using Dominodo.E2E.Clients.Modules.Tenants;
using Dominodo.E2E.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Dominodo.E2E.Tests.Tenants;

public abstract class BaseTenantsTests : BaseE2ETests
{
    // Resolved fresh per access (transient). The client is used ONLY inside Act blocks;
    // the builder drives the Arrange.
    protected ITenantsClient TenantsClient => ServiceProvider.GetRequiredService<ITenantsClient>();

    protected TenantsRequestBuilder TenantsRequestBuilder =>
        ServiceProvider.GetRequiredService<TenantsRequestBuilder>();
}
