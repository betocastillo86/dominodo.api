using System.Net;
using Dominodo.E2E.Core;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Memberships;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/memberships</c> (MembershipsController.List), guarded by
/// <c>[HasPermission(Permissions.MembershipsManage)]</c> and scoped by the <c>X-Tenant</c> header. The
/// permission resolves per (caller, resolved tenant) as platform ∪ the caller's Active-membership
/// permissions, so authorization is proven on both branches: a platform grant and a tenant grant.
/// Callers come from the API's IntegrationTests seed (one Platform user and one Tenant user per permission,
/// the latter with an Active membership in the fixed <see cref="DominodoConstants.IntegrationSeed.TenantSlug"/>).
/// </summary>
[TestFixture]
public sealed class GetMembershipsTests : BaseUsersTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await UsersClient.GetMemberships(tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksMembershipsManage()
    {
        // Arrange — a real, existing user assigned to a Platform role that carries ZERO permissions, against
        // a valid tenant so the 403 is unambiguously "missing memberships.manage" (not a bad/missing tenant).
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await UsersClient.GetMemberships(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenTenantManagerTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds memberships.manage only via an Active membership in the
        // seeded tenant. Targeting a *different* (freshly created) tenant resolves to only its platform
        // permissions (none), so authorization fails closed: tenant isolation ⇒ 403.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.MembershipsManage);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await UsersClient.GetMemberships(tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _200_WhenUserHasMembershipsManageInTenant()
    {
        // Arrange — the seeded tenant user: memberships.manage comes from its Active membership in the
        // seeded tenant (not a platform grant), so this exercises the tenant branch of resolution.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.MembershipsManage);
        var tenantUserId = DominodoConstants.IntegrationSeed.TenantUserIdFor(
            DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.GetMemberships(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        // The caller's own membership is listed for its tenant (scoped read).
        response.Content!.Items.ShouldContain(m => m.UserId == tenantUserId);
    }

    [Test]
    public async Task _200_WhenUserHasMembershipsManageOnPlatform()
    {
        // Arrange — the seeded Platform user holds memberships.manage cross-tenant, so it resolves for any
        // resolved tenant. This exercises the platform branch of resolution.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.GetMemberships(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
    }
}
