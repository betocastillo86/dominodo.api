using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Apartments.Residents;

/// <summary>
/// Black-box coverage for <c>DELETE /api/v1/apartments/{apartmentId}/residents/{residentId}</c>
/// (ApartmentResidentsController.Remove), guarded by <c>[HasPermission(Permissions.ApartmentsEdit)]</c> and
/// scoped by the <c>X-Tenant</c> header. Hard-deletes the residency row; success is 204 NoContent.
/// Authorization is proven on both branches (a Platform grant and a Tenant grant), that residency is not a
/// write grant, plus the tenant-mismatch paths (unknown slug ⇒ 400, wrong tenant ⇒ 403). The endpoint takes
/// no body, so there is no <c>Validation.Failed</c> case.
/// </summary>
[TestFixture]
public sealed class DeleteResidentTests : BaseTenantsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await TenantsClient.RemoveResident(
            Guid.NewGuid(), Guid.NewGuid(), tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksApartmentsEdit()
    {
        // Arrange — a bearer carrying apartments.view (not apartments.edit): proves removing a residency needs
        // the edit permission, distinct from the read permission.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.RemoveResident(
            Guid.NewGuid(), Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenResidentTriesToRemoveResidencyInTheirApartment()
    {
        // Arrange — a resident of the apartment. Residency grants READ access (GetById is dual-mode) but NOT
        // write: removing a residency requires apartments.edit, which the resident does not hold. Proves a
        // resident cannot remove residencies in their own apartment.
        var resident = await TenantsRequestBuilder.CreateResidentAsync();
        var token = JwtTokenFactory.CreateUserToken(resident.UserId);

        // Act — the resident tries to remove their own residency.
        var response = await TenantsClient.RemoveResident(
            resident.ApartmentId, resident.ResidentId, tenant: resident.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds apartments.edit only via its Active membership in the seeded
        // tenant. Targeting a *different* tenant resolves to only its (empty) platform permissions, so
        // authorization fails closed: write isolation ⇒ 403.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsEdit);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await TenantsClient.RemoveResident(
            Guid.NewGuid(), Guid.NewGuid(), tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — a valid edit token, but an X-Tenant slug that does not resolve. TenantResolutionMiddleware
        // rejects before authorization or the handler run.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.RemoveResident(
            Guid.NewGuid(), Guid.NewGuid(), tenant: $"unknown-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _204_WhenUserHasApartmentsEditOnPlatform()
    {
        // Arrange — Platform apartments.edit holder (cross-tenant grant) removing a residency (tenant +
        // apartment + user + residency all provisioned in one line).
        var resident = await TenantsRequestBuilder.CreateResidentAsync();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.RemoveResident(
            resident.ApartmentId, resident.ResidentId, tenant: resident.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task _204_WhenUserHasApartmentsEditInTenant()
    {
        // Arrange — the seeded tenant user: apartments.edit comes from its Active membership in the seeded
        // tenant (not a platform grant), so this exercises the tenant branch of resolution. The residency must
        // live in that same seeded tenant.
        var resident = await TenantsRequestBuilder.CreateResidentAsync(tenantSlug: SeededTenantSlug);
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsEdit);

        // Act
        var response = await TenantsClient.RemoveResident(
            resident.ApartmentId, resident.ResidentId, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
