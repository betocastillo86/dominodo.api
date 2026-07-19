using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Features;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/tenants/{tenantId}/features</c>, guarded by
/// <c>[HasPermission(Permissions.TenantsView)]</c> on the TenantFeaturesController (Platform-scoped).
/// </summary>
[TestFixture]
public sealed class GetTenantFeaturesTests : BaseTenantsTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await TenantsClient.GetTenantFeatures(Guid.NewGuid());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksTenantsView()
    {
        // Arrange — bearer carrying tenants.edit (not tenants.view): proves listing needs tenants.view.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.GetTenantFeatures(Guid.NewGuid(), token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _404_WhenTenantDoesNotExist()
    {
        // Arrange
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);

        // Act
        var response = await TenantsClient.GetTenantFeatures(Guid.NewGuid(), token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Tenant.NotFound");
    }

    [Test]
    public async Task _200_ReturnsFeatures()
    {
        // Arrange — create a tenant and enable a feature so the list has a known, asserted row.
        var created = await TenantsRequestBuilder.CreateTenantAsync();
        await TenantsRequestBuilder.SetTenantFeatureAsync(created.Id, "Deliveries", enabled: true);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);

        // Act
        var response = await TenantsClient.GetTenantFeatures(created.Id, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var features = response.Content;
        features.ShouldNotBeNull();
        var deliveries = features!.SingleOrDefault(f => f.FeatureKey == "Deliveries");
        deliveries.ShouldNotBeNull();
        deliveries!.Enabled.ShouldBeTrue();
        deliveries.TenantId.ShouldBe(created.Id);
    }
}
