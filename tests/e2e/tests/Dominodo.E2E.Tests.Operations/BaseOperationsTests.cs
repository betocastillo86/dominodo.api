using Dominodo.E2E.Clients.Modules.Operations;
using Dominodo.E2E.Clients.Modules.Tenants;
using Dominodo.E2E.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Dominodo.E2E.Tests.Operations;

public abstract class BaseOperationsTests : BaseE2ETests
{
    // Resolved fresh per access (transient). The client is used ONLY inside Act blocks;
    // the builder drives the Arrange.
    protected IOperationsClient OperationsClient => ServiceProvider.GetRequiredService<IOperationsClient>();

    // Composes announcement Arrange end to end (creating tenants as needed via the real APIs), so tests
    // never hand-roll setup.
    protected OperationsRequestBuilder OperationsRequestBuilder =>
        ServiceProvider.GetRequiredService<OperationsRequestBuilder>();

    // For the /mine feed: composes tenant + apartment + resident Arrange (the caller's audience).
    protected TenantsRequestBuilder TenantsRequestBuilder =>
        ServiceProvider.GetRequiredService<TenantsRequestBuilder>();
}
