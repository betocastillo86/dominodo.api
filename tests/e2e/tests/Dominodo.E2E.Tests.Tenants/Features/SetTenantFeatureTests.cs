using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Features;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/tenants/{tenantId}/features/{featureKey}</c>, guarded by
/// <c>[HasPermission(Permissions.TenantsEdit)]</c> on the TenantFeaturesController. Returns 204 NoContent.
/// The feature key travels in the route; only <c>Enabled</c> is in the body.
/// </summary>
[TestFixture]
public sealed class SetTenantFeatureTests : BaseTenantsTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = TenantsRequestBuilder.BuildSetTenantFeatureModel(enabled: true);

        // Act — no token
        var response = await TenantsClient.SetTenantFeature(Guid.NewGuid(), "Deliveries", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksTenantsEdit()
    {
        // Arrange — bearer carrying tenants.view (read-only): proves enabling a feature needs tenants.edit.
        var model = TenantsRequestBuilder.BuildSetTenantFeatureModel(enabled: true);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);

        // Act
        var response = await TenantsClient.SetTenantFeature(Guid.NewGuid(), "Deliveries", model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenFeatureKeyIsNotAValidEnum()
    {
        // Arrange — FeatureKey is the FeatureKey enum, bound from the route; an unknown name fails route
        // model binding and is mapped to the same Validation.Failed shape as a validator error.
        var model = TenantsRequestBuilder.BuildSetTenantFeatureModel(enabled: true);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.SetTenantFeature(Guid.NewGuid(), "NotAFeature", model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError("FeatureKey");
    }

    [Test]
    public async Task _404_WhenTenantDoesNotExist()
    {
        // Arrange — a valid feature key so validation passes and the handler's not-found path is reached.
        var model = TenantsRequestBuilder.BuildSetTenantFeatureModel(enabled: true);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.SetTenantFeature(Guid.NewGuid(), "Deliveries", model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Tenant.NotFound");
    }

    [Test]
    public async Task _204_SetsFeature_AndVerifiesByFetching()
    {
        // Arrange — a fresh tenant with no features yet.
        var created = await TenantsRequestBuilder.CreateTenantAsync();
        var model = TenantsRequestBuilder.BuildSetTenantFeatureModel(enabled: true);
        var editToken = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.SetTenantFeature(created.Id, "WhatsApp", model, editToken);

        // Assert — 204 NoContent (no body)
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify round-trip: the feature is listed as enabled.
        var viewToken = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);
        var getResponse = await TenantsClient.GetTenantFeatures(created.Id, viewToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var whatsApp = getResponse.Content!.SingleOrDefault(f => f.FeatureKey == "WhatsApp");
        whatsApp.ShouldNotBeNull();
        whatsApp!.Enabled.ShouldBeTrue();
    }
}
