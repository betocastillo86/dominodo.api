using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Tenants;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/tenants/{id}</c>, guarded by
/// <c>[HasPermission(Permissions.TenantsView)]</c> on the TenantsController.
/// </summary>
[TestFixture]
public sealed class GetTenantByIdTests : BaseTenantsTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await TenantsClient.GetTenantById(Guid.NewGuid());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksTenantsView()
    {
        // Arrange — bearer carrying tenants.edit (not tenants.view).
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.GetTenantById(Guid.NewGuid(), token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _404_WhenTenantDoesNotExist()
    {
        // Arrange
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);

        // Act
        var response = await TenantsClient.GetTenantById(Guid.NewGuid(), token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Tenant.NotFound");
    }

    [Test]
    public async Task _200_ReturnsTenant()
    {
        // Arrange — create a fresh tenant so the test is self-contained.
        var model = TenantsRequestBuilder.BuildNewTenantModel(type: "Edificio", legalId: "NIT-900123");
        var created = await TenantsRequestBuilder.CreateTenantAsync(model);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);

        // Act
        var response = await TenantsClient.GetTenantById(created.Id, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tenant = response.Content;
        tenant.ShouldNotBeNull();
        tenant!.Id.ShouldBe(created.Id);
        tenant.Slug.ShouldBe(model.Slug);
        tenant.Name.ShouldBe(model.Name);
        tenant.Type.ShouldBe(model.Type);
        tenant.LegalId.ShouldBe(model.LegalId);
        tenant.City.ShouldBe(model.City);
        tenant.Status.ShouldBe("Onboarding");
    }
}
