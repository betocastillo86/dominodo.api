using System.Net;
using Dominodo.E2E.Core;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Roles;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/roles</c>, guarded by
/// <c>[HasPermission(Permissions.RolesManage)]</c> on the RolesController. Permissions resolve
/// server-side from the DB by the token's subject, so a token for an unknown user has no
/// <c>roles.manage</c> and is forbidden; a token for the seeded <c>roles.manage</c> user is
/// authorized.
/// </summary>
[TestFixture]
public sealed class GetRolesTests : BaseUsersTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await UsersClient.GetRoles();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksRolesManage()
    {
        // Arrange — bearer for the seeded "Rol Public" user: exists and has a role, but zero permissions.
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await UsersClient.GetRoles(token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _200_ReturnsSeededRoles()
    {
        // Arrange — seeded user with roles.manage; server resolves the permission from DB by token sub.
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        // Act
        var response = await UsersClient.GetRoles(token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var paged = response.Content;
        paged.ShouldNotBeNull();
        paged!.Items.ShouldNotBeEmpty();
        paged.TotalCount.ShouldBeGreaterThan(0);

        // The SuperAdmin role is seeded as a system, Platform-scope role — assert its shape.
        paged.Items.ShouldContain(r => r.Name == "SuperAdmin");
        var superAdmin = paged.Items.First(r => r.Name == "SuperAdmin");
        superAdmin.IsSystem.ShouldBeTrue();
        superAdmin.Scope.ShouldBe("Platform");
    }
}
