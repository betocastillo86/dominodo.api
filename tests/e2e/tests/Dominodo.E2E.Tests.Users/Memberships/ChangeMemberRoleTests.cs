using System.Net;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Memberships;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/memberships/{id}/role</c> (MembershipsController.ChangeRole),
/// guarded by <c>[HasPermission(Permissions.MembershipsManage)]</c> and scoped by the <c>X-Tenant</c>
/// header. The membership is loaded ForCurrentTenant, so an admin can only re-role members of their own
/// conjunto. Authorization is proven on both branches (a platform grant and a tenant grant) plus its
/// tenant-isolation failure mode; the 204 flips the persisted RoleId.
/// </summary>
[TestFixture]
public sealed class ChangeMemberRoleTests : BaseUsersTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildChangeMemberRoleModel(1);

        // Act — no token
        var response = await UsersClient.ChangeMemberRole(Guid.NewGuid(), model, tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksMembershipsManage()
    {
        // Arrange — a real, existing user with ZERO permissions, against a valid tenant so the 403 is
        // unambiguously "missing memberships.manage".
        var token = JwtTokenFactory.GeneratePublicToken();
        var model = UsersRequestBuilder.BuildChangeMemberRoleModel(1);

        // Act
        var response = await UsersClient.ChangeMemberRole(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenRequestInvalid()
    {
        // Arrange — a caller that clears authorization (platform grant + valid tenant) so the request
        // reaches the validator, then break every rule in ChangeMemberRoleCommandValidator at once:
        // MembershipId (NotEmpty — Guid.Empty is a valid route guid but fails the rule) and RoleId
        // (GreaterThan(0)).
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);
        var model = UsersRequestBuilder.BuildChangeMemberRoleModel(0);

        // Act
        var response = await UsersClient.ChangeMemberRole(Guid.Empty, model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError("MembershipId")
                .ShouldHaveValidationError(nameof(ChangeMemberRoleModel.RoleId));
    }

    [Test]
    public async Task _404_WhenMembershipNotInTenant()
    {
        // Arrange — a platform manager (authorized for any tenant) targets a membership id that does not
        // exist in the resolved tenant. RoleId is positive so validation passes and the handler's
        // ForCurrentTenant lookup is what fails ⇒ 404.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);
        var model = UsersRequestBuilder.BuildChangeMemberRoleModel(1);

        // Act
        var response = await UsersClient.ChangeMemberRole(Guid.NewGuid(), model, tenant: SeededTenantSlug, token: token);

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
        var model = UsersRequestBuilder.BuildChangeMemberRoleModel(1);

        // Act
        var response = await UsersClient.ChangeMemberRole(Guid.NewGuid(), model, tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _204_WhenTenantManagerChangesRole()
    {
        // Arrange — a fresh member in the SEEDED tenant (where the seeded tenant user holds
        // memberships.manage via its Active membership) and a new Tenant-scope role to grant.
        var membership = await UsersRequestBuilder.InvitePlatformMembershipAsync(SeededTenantSlug);
        var newRole = await UsersRequestBuilder.CreateRoleAsync(scope: "Tenant");
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.MembershipsManage);
        var model = UsersRequestBuilder.BuildChangeMemberRoleModel(newRole.Id);

        // Act
        var response = await UsersClient.ChangeMemberRole(membership.MembershipId, model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Side effect: the member now carries the new role.
        var updated = await UsersRequestBuilder.FindMembershipAsync(SeededTenantSlug, membership.User.Id);
        updated.ShouldNotBeNull();
        updated!.RoleId.ShouldBe(newRole.Id);
    }

    [Test]
    public async Task _204_WhenPlatformManagerChangesRole()
    {
        // Arrange — the seeded Platform user holds memberships.manage cross-tenant, so it can re-role a
        // member in a brand-new (freshly created) tenant it has no membership in.
        var tenant = await TenantsRequestBuilder.CreateTenantAsync();
        var membership = await UsersRequestBuilder.InvitePlatformMembershipAsync(tenant.Slug);
        var newRole = await UsersRequestBuilder.CreateRoleAsync(scope: "Tenant");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);
        var model = UsersRequestBuilder.BuildChangeMemberRoleModel(newRole.Id);

        // Act
        var response = await UsersClient.ChangeMemberRole(membership.MembershipId, model, tenant: tenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Side effect: the member now carries the new role.
        var updated = await UsersRequestBuilder.FindMembershipAsync(tenant.Slug, membership.User.Id);
        updated.ShouldNotBeNull();
        updated!.RoleId.ShouldBe(newRole.Id);
    }
}
