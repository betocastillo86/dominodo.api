using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Tenants.Apartments;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/apartments</c> (ApartmentsController.List), guarded by
/// <c>[HasPermission(Permissions.ApartmentsView)]</c> and scoped by the <c>X-Tenant</c> header. Apartments
/// are a tenant-owned resource, so the permission resolves as platform ∪ the caller's Active-membership
/// permissions for the resolved tenant — authorization is proven on both branches. There is no request
/// validator and pagination is clamped in <c>PageRequest</c>, so there is no <c>400 Validation.Failed</c>
/// case; the only 400 is <c>Tenant.Unknown</c> from the resolution middleware.
/// </summary>
[TestFixture]
public sealed class GetAllApartmentsTests : BaseTenantsTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await TenantsClient.GetApartments(tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksApartmentsView()
    {
        // Arrange — a real, existing user assigned to a Platform role that carries ZERO permissions, against
        // a valid tenant so the 403 is unambiguously "missing apartments.view" (not a bad/missing tenant).
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await TenantsClient.GetApartments(tenant: SeededTenantSlug, token: token);

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
        var response = await TenantsClient.GetApartments(tenant: otherTenant.Slug, token: token);

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
        var response = await TenantsClient.GetApartments(tenant: $"unknown-{Guid.NewGuid():N}", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _200_WhenUserHasApartmentsViewInTenant()
    {
        // Arrange — the seeded tenant user: apartments.view comes from its Active membership in the seeded
        // tenant (not a platform grant), so this exercises the tenant branch of permission resolution.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartments(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
    }

    [Test]
    public async Task _200_WhenUserHasApartmentsViewOnPlatform()
    {
        // Arrange — the seeded Platform user holds apartments.view cross-tenant, so it resolves for any
        // resolved tenant. This exercises the platform branch of resolution.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartments(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
    }

    [Test]
    public async Task _200_ReturnsApartmentsScopedToTenant()
    {
        // Arrange — two apartments, each in its own fresh tenant (the builder creates the tenants). The list
        // must be scoped to the resolved tenant, so listing tenant A never leaks B's (the core guarantee).
        var a = await TenantsRequestBuilder.CreateApartmentAsync();
        var b = await TenantsRequestBuilder.CreateApartmentAsync();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartments(pageSize: 100, tenant: a.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content!.Items.ShouldContain(x => x.Id == a.Id);
        response.Content!.Items.ShouldNotContain(x => x.Id == b.Id);
    }

    [Test]
    public async Task _200_FiltersByTower()
    {
        // Arrange — two apartments in different towers of the SAME tenant (reuse the first's slug).
        var tower = $"T{Guid.NewGuid():N}";
        var inTower = await TenantsRequestBuilder.CreateApartmentAsync(tower: tower);
        var otherTower = await TenantsRequestBuilder.CreateApartmentAsync(
            tenantSlug: inTower.TenantSlug, tower: $"T{Guid.NewGuid():N}");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartments(
            tower: tower, pageSize: 100, tenant: inTower.TenantSlug, token: token);

        // Assert — only the matching-tower apartment is returned.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content!.Items.ShouldContain(a => a.Id == inTower.Id);
        response.Content!.Items.ShouldNotContain(a => a.Id == otherTower.Id);
    }

    [Test]
    public async Task _200_FiltersByType()
    {
        // Arrange — a House and an Apartment in the SAME tenant.
        var house = await TenantsRequestBuilder.CreateApartmentAsync(type: "House");
        var apartment = await TenantsRequestBuilder.CreateApartmentAsync(
            tenantSlug: house.TenantSlug, type: "Apartment");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act
        var response = await TenantsClient.GetApartments(
            type: "House", pageSize: 100, tenant: house.TenantSlug, token: token);

        // Assert — only the House is returned.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content!.Items.ShouldContain(a => a.Id == house.Id);
        response.Content!.Items.ShouldNotContain(a => a.Id == apartment.Id);
    }

    [Test]
    public async Task _200_PaginatesResults()
    {
        // Arrange — three apartments in the SAME fresh tenant, so the page window and total are deterministic.
        var first = await TenantsRequestBuilder.CreateApartmentAsync();
        await TenantsRequestBuilder.CreateApartmentAsync(tenantSlug: first.TenantSlug);
        await TenantsRequestBuilder.CreateApartmentAsync(tenantSlug: first.TenantSlug);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.ApartmentsView);

        // Act — first page of size 2.
        var response = await TenantsClient.GetApartments(
            page: 1, pageSize: 2, tenant: first.TenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content!.Page.ShouldBe(1);
        response.Content!.PageSize.ShouldBe(2);
        response.Content!.TotalCount.ShouldBe(3);
        response.Content!.Items.Count.ShouldBe(2);
    }
}
