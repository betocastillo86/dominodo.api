using System.Net;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Tenants;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/tenants/{id}/status</c>, guarded by
/// <c>[HasPermission(Permissions.TenantsEdit)]</c> on the TenantsController. Returns 204 NoContent.
/// </summary>
[TestFixture]
public sealed class ChangeTenantStatusTests : BaseTenantsTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = new ChangeTenantStatusModel { Status = "Active" };

        // Act — no token
        var response = await TenantsClient.ChangeTenantStatus(Guid.NewGuid(), model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksTenantsEdit()
    {
        // Arrange — bearer carrying tenants.view (read-only): proves the status change needs tenants.edit.
        var model = new ChangeTenantStatusModel { Status = "Active" };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsView);

        // Act
        var response = await TenantsClient.ChangeTenantStatus(Guid.NewGuid(), model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenStatusIsNotAValidEnum()
    {
        // Arrange — Status is the TenantStatus enum; an unknown name is rejected at JSON binding and mapped
        // to the same Validation.Failed shape as a validator error.
        var model = new ChangeTenantStatusModel { Status = "NotAStatus" };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.ChangeTenantStatus(Guid.NewGuid(), model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(ChangeTenantStatusModel.Status));
    }

    [Test]
    public async Task _404_WhenTenantDoesNotExist()
    {
        // Arrange — a valid status so validation passes and the handler's not-found path is reached.
        var model = new ChangeTenantStatusModel { Status = "Active" };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.ChangeTenantStatus(Guid.NewGuid(), model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Tenant.NotFound");
    }

    [Test]
    public async Task _204_ChangesStatus_AndVerifiesByFetching()
    {
        // Arrange — a fresh tenant starts in Onboarding; activate it.
        var created = await TenantsRequestBuilder.CreateTenantAsync();
        created.Status.ShouldBe("Onboarding");
        var model = new ChangeTenantStatusModel { Status = "Active" };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.TenantsEdit);

        // Act
        var response = await TenantsClient.ChangeTenantStatus(created.Id, model, token);

        // Assert — 204 NoContent (no body)
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify round-trip: the status flipped to Active.
        var tenant = await TenantsRequestBuilder.GetTenantAsync(created.Id);
        tenant.Status.ShouldBe("Active");
    }
}
