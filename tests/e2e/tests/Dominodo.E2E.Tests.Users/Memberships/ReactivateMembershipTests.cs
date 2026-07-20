using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Memberships;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/memberships/{id}/reactivate</c> (MembershipsController.Reactivate),
/// guarded by <c>[HasPermission(Permissions.MembershipsManage)]</c> and scoped by the <c>X-Tenant</c>
/// header. The membership is loaded ForCurrentTenant, so an admin can only reactivate members of their own
/// conjunto. Authorization is proven on both branches (platform and tenant grant) plus its tenant-isolation
/// failure mode; the state machine only allows reactivating a <c>Suspended</c> membership (else 409), and
/// the 204 flips the persisted Status back to Active.
/// </summary>
[TestFixture]
public sealed class ReactivateMembershipTests : BaseUsersTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await UsersClient.ReactivateMembership(Guid.NewGuid(), tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksMembershipsManage()
    {
        // Arrange — a real, existing user with ZERO permissions, against a valid tenant so the 403 is
        // unambiguously "missing memberships.manage".
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await UsersClient.ReactivateMembership(Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenMembershipIdEmpty()
    {
        // Arrange — a caller that clears authorization (platform grant + valid tenant) so the request
        // reaches the validator. Guid.Empty is a valid route guid but fails MembershipId.NotEmpty().
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.ReactivateMembership(Guid.Empty, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError("MembershipId");
    }

    [Test]
    public async Task _404_WhenMembershipNotInTenant()
    {
        // Arrange — a platform manager (authorized for any tenant) targets a membership id that does not
        // exist in the resolved tenant ⇒ the ForCurrentTenant lookup fails ⇒ 404.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.ReactivateMembership(Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Membership.NotFound");
    }

    [Test]
    public async Task _409_WhenMembershipNotSuspended()
    {
        // Arrange — a fresh ACTIVE (never suspended) membership in a new tenant. Only a suspended membership
        // can be reactivated ⇒ 409 Membership.NotSuspended.
        var tenant = await TenantsRequestBuilder.CreateTenantAsync();
        var membership = await UsersRequestBuilder.ArrangeActiveMembershipAsync(tenant.Slug);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.ReactivateMembership(membership.MembershipId, tenant: tenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.ShouldHaveErrorCode("Membership.NotSuspended");
    }

    [Test]
    public async Task _403_WhenTenantManagerTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds memberships.manage only via an Active membership in the
        // seeded tenant. Targeting a *different* (freshly created) tenant resolves to only its platform
        // permissions (none), so authorization fails closed before the handler runs: tenant isolation ⇒ 403.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.MembershipsManage);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();

        // Act
        var response = await UsersClient.ReactivateMembership(Guid.NewGuid(), tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _204_WhenTenantManagerReactivates()
    {
        // Arrange — a fresh SUSPENDED member in the SEEDED tenant (invited, accepted, then suspended), where
        // the seeded tenant user holds memberships.manage via its own Active membership.
        var membership = await UsersRequestBuilder.ArrangeSuspendedMembershipAsync(SeededTenantSlug);
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.ReactivateMembership(membership.MembershipId, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Side effect: the member is Active again.
        var updated = await UsersRequestBuilder.FindMembershipAsync(SeededTenantSlug, membership.User.Id);
        updated.ShouldNotBeNull();
        updated!.Status.ShouldBe("Active");
    }

    [Test]
    public async Task _204_WhenPlatformManagerReactivates()
    {
        // Arrange — the seeded Platform user holds memberships.manage cross-tenant, so it can reactivate a
        // suspended member in a brand-new (freshly created) tenant it has no membership in.
        var tenant = await TenantsRequestBuilder.CreateTenantAsync();
        var membership = await UsersRequestBuilder.ArrangeSuspendedMembershipAsync(tenant.Slug);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.ReactivateMembership(membership.MembershipId, tenant: tenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Side effect: the member is Active again.
        var updated = await UsersRequestBuilder.FindMembershipAsync(tenant.Slug, membership.User.Id);
        updated.ShouldNotBeNull();
        updated!.Status.ShouldBe("Active");
    }
}
