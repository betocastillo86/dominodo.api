using System.Net;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Tenants;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/tenants/{id}</c>, guarded by
/// <c>[HasPermission(Permissions.TenantsEdit)]</c> on the TenantsController. Returns 204 NoContent.
/// </summary>
[TestFixture]
public sealed class UpdateTenantTests : BaseTenantsTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = TenantsRequestBuilder.BuildUpdateTenantModel();

        // Act — no token
        var response = await TenantsClient.UpdateTenant(Guid.NewGuid(), model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksTenantsEdit()
    {
        // Arrange — bearer carrying tenants.view (read-only): proves the write needs tenants.edit.
        var model = TenantsRequestBuilder.BuildUpdateTenantModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);

        // Act
        var response = await TenantsClient.UpdateTenant(Guid.NewGuid(), model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenAllValidationRulesViolated()
    {
        // Arrange — break every UpdateTenantCommandValidator rule at once:
        // Name NotEmpty, and the profile MaximumLength rules (LegalId 50, Address 300, City 100, Country 100).
        // Validation runs before the handler, so the (non-existent) id never reaches the 404 path.
        var model = TenantsRequestBuilder.BuildUpdateTenantModel() with
        {
            Name = "",
            LegalId = new string('x', 51),
            Address = new string('x', 301),
            City = new string('x', 101),
            Country = new string('x', 101),
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.UpdateTenant(Guid.NewGuid(), model, token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateTenantModel.Name))
                .ShouldHaveValidationError(nameof(UpdateTenantModel.LegalId))
                .ShouldHaveValidationError(nameof(UpdateTenantModel.Address))
                .ShouldHaveValidationError(nameof(UpdateTenantModel.City))
                .ShouldHaveValidationError(nameof(UpdateTenantModel.Country));
    }

    [Test]
    public async Task _404_WhenTenantDoesNotExist()
    {
        // Arrange — a valid body so validation passes and the handler's not-found path is reached.
        var model = TenantsRequestBuilder.BuildUpdateTenantModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.UpdateTenant(Guid.NewGuid(), model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Tenant.NotFound");
    }

    [Test]
    public async Task _204_UpdatesTenant_AndVerifiesByFetching()
    {
        // Arrange — create a fresh tenant, then build an update with a new name and profile.
        var created = await TenantsRequestBuilder.CreateTenantAsync();
        var updateModel = TenantsRequestBuilder.BuildUpdateTenantModel(
            name: "Conjunto Renombrado", legalId: "NIT-902777", city: "Medellín");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.UpdateTenant(created.Id, updateModel, token);

        // Assert — 204 NoContent (no body)
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify round-trip: the tenant reflects the update. Slug is immutable, so it stays the same.
        var tenant = await TenantsRequestBuilder.GetTenantAsync(created.Id);
        tenant.Slug.ShouldBe(created.Slug);
        tenant.Name.ShouldBe(updateModel.Name);
        tenant.LegalId.ShouldBe(updateModel.LegalId);
        tenant.Address.ShouldBe(updateModel.Address);
        tenant.City.ShouldBe(updateModel.City);
        tenant.Country.ShouldBe(updateModel.Country);
    }
}
