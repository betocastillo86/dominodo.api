using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Apartments;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/apartments/{id}</c> (ApartmentsController.GetById), scoped by the
/// <c>X-Tenant</c> header and guarded by a plain <c>[Authorize]</c> in <b>dual mode</b>: the caller reads the
/// apartment if they hold <c>apartments.view</c> (staff, any apartment) OR are an active resident of that
/// apartment. Denial is a leak-safe <c>404 Apartment.NotFound</c> — identical to a missing row, so existence
/// is never disclosed. There is therefore no 403 for this endpoint.
/// </summary>
[TestFixture]
public sealed class GetApartmentByIdTests : BaseTenantsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token (auth is checked before anything, so no apartment need exist)
        var response = await TenantsClient.GetApartmentById(Guid.NewGuid(), tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _404_WhenCallerHasNeitherViewNorResidency()
    {
        // Arrange — the apartment exists (builder creates its tenant too), but the caller is a real user with
        // zero permissions and no residency. Dual-mode denial is leak-safe: 404, not 403.
        var apartment = await TenantsRequestBuilder.CreateApartmentAsync();
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await TenantsClient.GetApartmentById(apartment.Id, tenant: apartment.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Apartment.NotFound");
    }

    [Test]
    public async Task _200_WhenUserHasApartmentsView()
    {
        // Arrange — an apartment (+ its tenant), read by a Platform apartments.view holder (cross-tenant grant).
        var apartment = await TenantsRequestBuilder.CreateApartmentAsync();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartmentById(apartment.Id, tenant: apartment.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldBe(apartment.Id);
    }

    [Test]
    public async Task _200_WhenUserHasApartmentsViewInTenant()
    {
        // Arrange — the apartment must live in the seeded tenant, because the seeded tenant user's
        // apartments.view grant resolves only there (its Active membership). This exercises the tenant branch.
        var apartment = await TenantsRequestBuilder.CreateApartmentAsync(tenantSlug: SeededTenantSlug);
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartmentById(apartment.Id, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldBe(apartment.Id);
    }

    [Test]
    public async Task _200_WhenCallerIsActiveResident()
    {
        // Arrange — one line builds tenant + apartment + user + active residency. The token is for the
        // resident user (no apartments.view), so the resident branch of the dual-mode check grants access.
        var resident = await TenantsRequestBuilder.CreateResidentAsync();
        var token = JwtTokenFactory.CreateUserToken(resident.UserId);

        // Act
        var response = await TenantsClient.GetApartmentById(
            resident.ApartmentId, tenant: resident.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldBe(resident.ApartmentId);
    }
}
