using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Apartments.Residents;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/apartments/{apartmentId}/residents</c>
/// (ApartmentResidentsController.List), guarded by <c>[HasPermission(Permissions.ApartmentsView)]</c> and
/// scoped by the <c>X-Tenant</c> header. Returns the apartment's residents as a plain list. Authorization is
/// proven on both branches (a Platform grant and a Tenant grant), plus the tenant-mismatch paths (unknown
/// slug ⇒ 400, wrong tenant ⇒ 403) and an unknown apartment ⇒ 404. There is no request validator, so there
/// is no <c>Validation.Failed</c> case.
/// </summary>
[TestFixture]
public sealed class GetApartmentResidentsTests : BaseTenantsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await TenantsClient.GetApartmentResidents(Guid.NewGuid(), tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksApartmentsView()
    {
        // Arrange — a real, existing user assigned to a Platform role that carries ZERO permissions, against a
        // valid tenant so the 403 is unambiguously "missing apartments.view" (not a bad/missing tenant).
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await TenantsClient.GetApartmentResidents(Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantUserTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds apartments.view only via an Active membership in the seeded
        // tenant. Targeting a *different* (freshly created) tenant resolves to only its platform permissions
        // (none), so authorization fails closed: tenant isolation ⇒ 403.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsView);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await TenantsClient.GetApartmentResidents(Guid.NewGuid(), tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — a valid apartments.view token, but an X-Tenant slug that does not resolve to any tenant.
        // TenantResolutionMiddleware rejects before authorization even runs.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartmentResidents(
            Guid.NewGuid(), tenant: $"unknown-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _404_WhenApartmentDoesNotExist()
    {
        // Arrange — a valid apartments.view token and a resolvable tenant, but an id that exists in no
        // apartment (scoped to the resolved tenant).
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartmentResidents(Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Apartment.NotFound");
    }

    [Test]
    public async Task _200_WhenUserHasApartmentsViewOnPlatform_ReturnsResidents()
    {
        // Arrange — Platform apartments.view holder (cross-tenant grant) listing an apartment that has a
        // resident (tenant + apartment + user + residency provisioned in one line).
        var resident = await TenantsRequestBuilder.CreateResidentAsync(relationType: "Renter");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartmentResidents(
            resident.ApartmentId, tenant: resident.TenantSlug, token: token);

        // Assert — the residency is listed with the expected shape.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        var listed = response.Content!.ShouldHaveSingleItem();
        listed.Id.ShouldBe(resident.ResidentId);
        listed.UserId.ShouldBe(resident.UserId);
        listed.RelationType.ShouldBe("Renter");
        listed.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task _200_WhenUserHasApartmentsViewInTenant()
    {
        // Arrange — the seeded tenant user: apartments.view comes from its Active membership in the seeded
        // tenant (not a platform grant), so this exercises the tenant branch of resolution. The apartment and
        // residency must live in that same seeded tenant.
        var resident = await TenantsRequestBuilder.CreateResidentAsync(tenantSlug: SeededTenantSlug);
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartmentResidents(
            resident.ApartmentId, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        response.Content!.ShouldContain(r => r.Id == resident.ResidentId);
    }
}
