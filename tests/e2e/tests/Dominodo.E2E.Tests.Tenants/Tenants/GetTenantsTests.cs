using System.Net;
using Dominodo.E2E.Core;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Tenants;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/tenants</c>, guarded by
/// <c>[HasPermission(Permissions.TenantsView)]</c> on the TenantsController (Platform-scoped, no X-Tenant).
/// </summary>
[TestFixture]
public sealed class GetTenantsTests : BaseTenantsTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await TenantsClient.GetTenants();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksTenantsView()
    {
        // Arrange — a bearer that carries a *different* tenant permission (tenants.edit) but NOT
        // tenants.view: proves only tenants.view unlocks the read.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.GetTenants(token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _200_ReturnsTenants()
    {
        // Arrange — create a fresh tenant so the list is guaranteed non-empty.
        await TenantsRequestBuilder.CreateTenantAsync();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);

        // Act
        var response = await TenantsClient.GetTenants(token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        response.Content!.Items.ShouldNotBeEmpty();
    }
}
