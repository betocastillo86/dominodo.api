using System.Net;
using Dominodo.E2E.Clients.Modules.Tenants.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Apartments;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/apartments/{id}/status</c> (ApartmentsController.ChangeStatus),
/// guarded by <c>[HasPermission(Permissions.ApartmentsEdit)]</c> and scoped by the <c>X-Tenant</c> header.
/// Success is 204 NoContent. Authorization is proven on both branches (a Platform grant and a Tenant grant),
/// residency is not a write grant (403), plus the tenant-mismatch paths (unknown slug ⇒ 400, wrong tenant ⇒
/// 403), an unknown id ⇒ 404, an invalid status ⇒ 400, and a no-op transition ⇒ 409. New apartments start
/// Vacant, so "Occupied" is a valid transition and re-setting "Vacant" is the conflict.
/// </summary>
[TestFixture]
public sealed class ChangeApartmentStatusTests : BaseTenantsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel();

        // Act — no token
        var response = await TenantsClient.ChangeApartmentStatus(Guid.NewGuid(), model, tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksApartmentsEdit()
    {
        // Arrange — a bearer carrying apartments.view (not apartments.edit): proves the mutation needs the
        // edit permission, distinct from the read permission.
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.ChangeApartmentStatus(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenResidentTriesToChangeStatus()
    {
        // Arrange — a resident of the apartment. Residency grants READ access (GetById is dual-mode) but NOT
        // status changes: this endpoint requires apartments.edit, which the resident does not hold.
        var resident = await TenantsRequestBuilder.CreateResidentAsync();
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel();
        var token = JwtTokenFactory.CreateUserToken(resident.UserId);

        // Act
        var response = await TenantsClient.ChangeApartmentStatus(
            resident.ApartmentId, model, tenant: resident.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds apartments.edit only via its Active membership in the seeded
        // tenant. Targeting a *different* tenant resolves to only its (empty) platform permissions, so
        // authorization fails closed: write isolation ⇒ 403.
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsEdit);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await TenantsClient.ChangeApartmentStatus(Guid.NewGuid(), model, tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — a valid edit token, but an X-Tenant slug that does not resolve. TenantResolutionMiddleware
        // rejects before authorization or the handler run.
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.ChangeApartmentStatus(
            Guid.NewGuid(), model, tenant: $"unknown-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _400_WhenStatusInvalid()
    {
        // Arrange — Status is the ApartmentStatus enum; an unknown name is rejected at JSON binding and
        // mapped to the same Validation.Failed shape as a validator error.
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel(status: "NotAStatus");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.ChangeApartmentStatus(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(ChangeApartmentStatusModel.Status));
    }

    [Test]
    public async Task _404_WhenApartmentDoesNotExist()
    {
        // Arrange — a valid edit token and a resolvable tenant, but an id that exists in no apartment (scoped
        // to the resolved tenant).
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.ChangeApartmentStatus(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Apartment.NotFound");
    }

    [Test]
    public async Task _204_WhenUserHasApartmentsEditOnPlatform_AndVerifiesByFetching()
    {
        // Arrange — Platform apartments.edit holder (cross-tenant grant); flip the new (Vacant) apartment to
        // Occupied.
        var apartment = await TenantsRequestBuilder.CreateApartmentAsync();
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel(status: "Occupied");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.ChangeApartmentStatus(apartment.Id, model, tenant: apartment.TenantSlug, token: token);

        // Assert — 204, and the status is persisted.
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var updated = await TenantsRequestBuilder.GetApartmentAsync(apartment.TenantSlug, apartment.Id);
        updated.Status.ShouldBe("Occupied");
    }

    [Test]
    public async Task _204_WhenUserHasApartmentsEditInTenant()
    {
        // Arrange — the seeded tenant user: apartments.edit comes from its Active membership in the seeded
        // tenant (not a platform grant), so this exercises the tenant branch of resolution.
        var apartment = await TenantsRequestBuilder.CreateApartmentAsync(tenantSlug: SeededTenantSlug);
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel(status: "Occupied");
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.ChangeApartmentStatus(apartment.Id, model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task _409_WhenStatusUnchanged()
    {
        // Arrange — a new apartment is already Vacant; re-setting Vacant is a no-op transition the aggregate
        // rejects.
        var apartment = await TenantsRequestBuilder.CreateApartmentAsync();
        var model = TenantsRequestBuilder.BuildChangeApartmentStatusModel(status: "Vacant");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.ChangeApartmentStatus(apartment.Id, model, tenant: apartment.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.ShouldHaveErrorCode("Apartment.AlreadyVacant");
    }
}
