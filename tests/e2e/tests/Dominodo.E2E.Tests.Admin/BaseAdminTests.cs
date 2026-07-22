using Dominodo.E2E.Clients.Modules.Admin;
using Dominodo.E2E.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Dominodo.E2E.Tests.Admin;

public abstract class BaseAdminTests : BaseE2ETests
{
    // Resolved fresh per access (transient). The client is used ONLY inside Act blocks;
    // the builder drives the Arrange.
    protected IAdminClient AdminClient => ServiceProvider.GetRequiredService<IAdminClient>();

    protected AdminRequestBuilder AdminRequestBuilder =>
        ServiceProvider.GetRequiredService<AdminRequestBuilder>();
}
