using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Roles;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/roles/{id}</c>, guarded by
/// <c>[HasPermission(Permissions.RolesManage)]</c> on the RolesController.
/// </summary>
[TestFixture]
public sealed class GetRoleByIdTests : BaseUsersTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await UsersClient.GetRoleById(1);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksRolesManage()
    {
        // Arrange — valid bearer for an unknown user; server resolves an empty permission set.
        var token = JwtTokenFactory.CreateUserToken(Guid.NewGuid());

        // Act
        var response = await UsersClient.GetRoleById(1, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _404_WhenRoleDoesNotExist()
    {
        // Arrange
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        // Act
        var response = await UsersClient.GetRoleById(int.MaxValue, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Role.NotFound");
    }

    [Test]
    public async Task _200_ReturnsRole()
    {
        // Arrange — create a fresh role so the test is self-contained.
        var model = UsersRequestBuilder.BuildNewRoleModel();
        var id = await UsersRequestBuilder.CreateRoleAsync(model);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        // Act
        var response = await UsersClient.GetRoleById(id, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var role = response.Content;
        role.ShouldNotBeNull();
        role!.Id.ShouldBe(id);
        role.Name.ShouldBe(model.Name);
        role.Description.ShouldBe(model.Description);
        role.Scope.ShouldBe(model.Scope);
        role.IsSystem.ShouldBeFalse();
        role.Permissions.ShouldNotBeEmpty();
        role.Permissions[0].Code.ShouldBe(DominodoConstants.Permission.UsersManage);
    }
}
