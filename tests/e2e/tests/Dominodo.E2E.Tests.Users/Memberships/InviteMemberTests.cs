using System.Net;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Memberships;

/// <summary>
/// Black-box coverage for <c>POST /api/v1/memberships/invite</c> (MembershipsController.Invite), guarded
/// by <c>[HasPermission(Permissions.MembershipsManage)]</c> and scoped by the <c>X-Tenant</c> header. The
/// permission resolves per (caller, resolved tenant) as platform ∪ the caller's Active-membership
/// permissions, so authorization is proven on both branches (a platform grant and a tenant grant) and its
/// tenant-isolation failure mode. Callers come from the API's IntegrationTests seed. The tenant is resolved
/// by <c>TenantResolutionMiddleware</c> BEFORE authorization: an unknown slug is <b>400 Tenant.Unknown</b>
/// (not 403), while a valid but foreign tenant fails closed at authorization ⇒ 403.
/// </summary>
[TestFixture]
public sealed class InviteMemberTests : BaseUsersTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildInviteMemberModel();

        // Act — no token
        var response = await UsersClient.InviteMember(model, tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksMembershipsManage()
    {
        // Arrange — a real, existing user assigned to a Platform role that carries ZERO permissions,
        // against a valid tenant so the 403 is unambiguously "missing memberships.manage".
        var token = JwtTokenFactory.GeneratePublicToken();
        var model = UsersRequestBuilder.BuildInviteMemberModel();

        // Act
        var response = await UsersClient.InviteMember(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenRequestInvalid()
    {
        // Arrange — a caller that clears authorization (platform grant + valid tenant) so the request
        // reaches the validator, then break every rule in InviteMemberCommandValidator at once:
        // Phone (NotEmpty + E.164 regex) and RoleId (GreaterThan(0)).
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);
        var model = UsersRequestBuilder.BuildInviteMemberModel() with { Phone = "", RoleId = 0 };

        // Act
        var response = await UsersClient.InviteMember(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(InviteMemberModel.Phone))
                .ShouldHaveValidationError(nameof(InviteMemberModel.RoleId));
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — an unknown tenant slug is rejected by TenantResolutionMiddleware BEFORE authorization,
        // so an otherwise-authorized caller still gets 400 Tenant.Unknown (NOT 403 — the resolution
        // middleware fails first). A valid body isolates the failure to the tenant.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);
        var model = UsersRequestBuilder.BuildInviteMemberModel();
        var unknownSlug = $"e2e-unknown-{Guid.NewGuid():N}";

        // Act
        var response = await UsersClient.InviteMember(model, tenant: unknownSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _201_WhenTenantManagerInvites()
    {
        // Arrange — the seeded tenant user holds memberships.manage only via an Active membership in the
        // seeded tenant (tenant branch of resolution). Invite a fresh user into that same tenant.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.MembershipsManage);
        var model = await UsersRequestBuilder.ArrangeInviteMemberModelAsync();

        // Act
        var response = await UsersClient.InviteMember(model, tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task _201_WhenPlatformManagerInvites()
    {
        // Arrange — the seeded Platform user holds memberships.manage cross-tenant (platform branch of
        // resolution), so it can invite into any resolved tenant. Prove that by inviting into a brand-new
        // (freshly created, resolvable) tenant it has no membership in.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.MembershipsManage);
        var tenant = await TenantsRequestBuilder.CreateTenantAsync();
        var model = await UsersRequestBuilder.ArrangeInviteMemberModelAsync();

        // Act
        var response = await UsersClient.InviteMember(model, tenant: tenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task _403_WhenTenantManagerTargetsAnotherTenant()
    {
        // Arrange — the seeded tenant user holds memberships.manage only via an Active membership in the
        // seeded tenant. Targeting a *different* (freshly created, resolvable) tenant resolves to only its
        // platform permissions (none), so authorization fails closed: tenant isolation ⇒ 403.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.MembershipsManage);
        var otherTenant = await TenantsRequestBuilder.CreateTenantAsync();
        var model = await UsersRequestBuilder.ArrangeInviteMemberModelAsync();

        // Act
        var response = await UsersClient.InviteMember(model, tenant: otherTenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
