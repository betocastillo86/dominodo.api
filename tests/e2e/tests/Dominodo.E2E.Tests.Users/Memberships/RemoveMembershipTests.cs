using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Memberships;

/// <summary>
/// Black-box coverage for <c>DELETE /api/v1/memberships/{id}</c> (MembershipsController.Remove), guarded by
/// <c>[HasPermission(Permissions.MembershipsManage)]</c> and scoped by the <c>X-Tenant</c> header. The
/// membership is loaded ForCurrentTenant, so an admin can only remove members of their own conjunto —
/// cross-tenant removal is impossible. It hard-deletes the row regardless of status (no state machine),
/// so the only failure modes are authorization, tenant isolation, and a missing membership (404). The 204
/// removes the row.
/// </summary>
[TestFixture]
public sealed class RemoveMembershipTests : BaseUsersTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await UsersClient.RemoveMembership(Guid.NewGuid(), tenant: SeededTenantSlug);

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
        var response = await UsersClient.RemoveMembership(Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

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
        var response = await UsersClient.RemoveMembership(Guid.Empty, tenant: SeededTenantSlug, token: token);

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
        var response = await UsersClient.RemoveMembership(Guid.NewGuid(), tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Membership.NotFound");
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
        var response = await UsersClient.RemoveMembership(Guid.NewGuid(), tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _204_WhenTenantManagerRemoves()
    {
        // Arrange — a fresh member in the SEEDED tenant (remove works on any status, so an Invited one is
        // enough), where the seeded tenant user holds memberships.manage via its own Active membership.
        var membership = await UsersRequestBuilder.InvitePlatformMembershipAsync(SeededTenantSlug);
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.RemoveMembership(membership.MembershipId, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Side effect: the membership is gone.
        var removed = await UsersRequestBuilder.FindMembershipAsync(SeededTenantSlug, membership.User.Id);
        removed.ShouldBeNull();
    }

    [Test]
    public async Task _204_WhenPlatformManagerRemoves()
    {
        // Arrange — the seeded Platform user holds memberships.manage cross-tenant, so it can remove a
        // member in a brand-new (freshly created) tenant it has no membership in.
        var tenant = await TenantsRequestBuilder.CreateTenantAsync();
        var membership = await UsersRequestBuilder.InvitePlatformMembershipAsync(tenant.Slug);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.RemoveMembership(membership.MembershipId, tenant: tenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Side effect: the membership is gone.
        var removed = await UsersRequestBuilder.FindMembershipAsync(tenant.Slug, membership.User.Id);
        removed.ShouldBeNull();
    }
}
