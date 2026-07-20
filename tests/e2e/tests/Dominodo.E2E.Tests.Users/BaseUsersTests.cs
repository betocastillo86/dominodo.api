using Dominodo.E2E.Clients.Modules.Tenants;
using Dominodo.E2E.Clients.Modules.Users;
using Dominodo.E2E.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Dominodo.E2E.Tests.Users;

public abstract class BaseUsersTests : BaseE2ETests
{
    // Resolved fresh per access (transient). The client is used ONLY inside Act blocks;
    // the builder drives the Arrange.
    protected IUsersClient UsersClient => ServiceProvider.GetRequiredService<IUsersClient>();

    protected UsersRequestBuilder UsersRequestBuilder => ServiceProvider.GetRequiredService<UsersRequestBuilder>();

    // Membership tests need a second, foreign tenant to prove tenant isolation; the Tenants builder
    // creates one via the real API (no hand-rolled tenant data).
    protected TenantsRequestBuilder TenantsRequestBuilder =>
        ServiceProvider.GetRequiredService<TenantsRequestBuilder>();
}
