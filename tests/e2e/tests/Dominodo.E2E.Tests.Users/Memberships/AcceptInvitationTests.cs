using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Memberships;

/// <summary>
/// Black-box coverage for <c>POST /api/v1/memberships/accept</c> (MembershipsController.Accept). Unlike the
/// other membership endpoints this is <b>self-service</b>: guarded by plain <c>[Authorize]</c> (any valid
/// bearer, no permission), it accepts the caller's OWN invitation in the resolved tenant. It is scoped by
/// the <c>X-Tenant</c> header (resolved before the handler ⇒ unknown slug is 400 Tenant.Unknown), and the
/// handler drives the membership state machine: no membership ⇒ 404 Membership.NotFound, a non-Invited one
/// ⇒ 409 Membership.NotInvited, an Invited one ⇒ 204 and the row flips to Active.
/// </summary>
[TestFixture]
public sealed class AcceptInvitationTests : BaseUsersTests
{
    private const string SeededTenantSlug = DominodoConstants.IntegrationSeed.TenantSlug;

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await UsersClient.AcceptInvitation(tenant: SeededTenantSlug);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _400_WhenTenantUnknown()
    {
        // Arrange — an unknown tenant slug is rejected by TenantResolutionMiddleware BEFORE the handler,
        // so even a valid bearer gets 400 Tenant.Unknown.
        var user = await UsersRequestBuilder.RegisterUserAsync();
        var token = JwtTokenFactory.CreateUserToken(user.Id);
        var unknownSlug = $"e2e-unknown-{Guid.NewGuid():N}";

        // Act
        var response = await UsersClient.AcceptInvitation(tenant: unknownSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Tenant.Unknown");
    }

    [Test]
    public async Task _404_WhenCallerHasNoInvitation()
    {
        // Arrange — a fresh, authenticated user with NO membership in the resolved tenant. Accept is not
        // permission-gated, so authorization passes; the handler finds no membership ⇒ 404.
        var user = await UsersRequestBuilder.RegisterUserAsync();
        var token = JwtTokenFactory.CreateUserToken(user.Id);

        // Act
        var response = await UsersClient.AcceptInvitation(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Membership.NotFound");
    }

    [Test]
    public async Task _409_WhenMembershipAlreadyActive()
    {
        // Arrange — the seeded tenant user's ONLY grant is an ALREADY-Active membership in the seeded
        // tenant. Accepting a non-Invited membership violates the state machine ⇒ 409 Membership.NotInvited.
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.MembershipsManage);

        // Act
        var response = await UsersClient.AcceptInvitation(tenant: SeededTenantSlug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.ShouldHaveErrorCode("Membership.NotInvited");
    }

    [Test]
    public async Task _204_WhenInvitedUserAcceptsOwnInvitation()
    {
        // Arrange — a fresh tenant with an Invited membership for a fresh user (arranged via the platform
        // invite path). The caller presents THAT user's own token (accept is self-service).
        var tenant = await TenantsRequestBuilder.CreateTenantAsync();
        var invited = await UsersRequestBuilder.InvitePlatformMembershipAsync(tenant.Slug);
        var token = JwtTokenFactory.CreateUserToken(invited.User.Id);

        // Act
        var response = await UsersClient.AcceptInvitation(tenant: tenant.Slug, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Side effect: the membership transitioned Invited → Active.
        var membership = await UsersRequestBuilder.FindMembershipAsync(tenant.Slug, invited.User.Id);
        membership.ShouldNotBeNull();
        membership!.Status.ShouldBe("Active");
    }
}
