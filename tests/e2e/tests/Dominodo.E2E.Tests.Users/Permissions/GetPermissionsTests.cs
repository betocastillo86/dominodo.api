using System.Net;
using Dominodo.E2E.Core;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Permissions;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/permissions</c>, guarded by
/// <c>[HasPermission(Permissions.RolesManage)]</c> on the PermissionsController.
/// Note: there is no 404 path — the handler always returns success (possibly an empty list).
/// </summary>
[TestFixture]
public sealed class GetPermissionsTests : BaseUsersTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await UsersClient.GetPermissions();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksRolesManage()
    {
        // Arrange — valid bearer for an unknown user; server resolves an empty permission set.
        var token = JwtTokenFactory.CreateUserToken(Guid.NewGuid());

        // Act
        var response = await UsersClient.GetPermissions(token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _200_ReturnsPermissions()
    {
        // Arrange
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        // Act
        var response = await UsersClient.GetPermissions(token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var permissions = response.Content;
        permissions.ShouldNotBeNull();
        permissions!.ShouldNotBeEmpty();
        permissions.ShouldAllBe(p => p.Id > 0);
        permissions.ShouldAllBe(p => !string.IsNullOrEmpty(p.Code));
        permissions.ShouldAllBe(p => !string.IsNullOrEmpty(p.Group));
    }
}
