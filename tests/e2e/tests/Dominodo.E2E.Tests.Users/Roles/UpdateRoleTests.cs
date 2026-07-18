using System.Net;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Roles;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/roles/{id}</c>, guarded by
/// <c>[HasPermission(Permissions.RolesManage)]</c> on the RolesController.
/// Returns 204 NoContent on success.
/// </summary>
[TestFixture]
public sealed class UpdateRoleTests : BaseUsersTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildUpdateRoleModel();

        // Act — no token
        var response = await UsersClient.UpdateRole(1, model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksRolesManage()
    {
        // Arrange — bearer for the seeded "Rol Public" user: exists and has a role, but zero permissions.
        var model = UsersRequestBuilder.BuildUpdateRoleModel();
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await UsersClient.UpdateRole(1, model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenAllValidationRulesViolated()
    {
        // Arrange — break every UpdateRoleCommandValidator rule at once:
        // Name NotEmpty, Description MaximumLength(300), PermissionIds NotNull.
        var id = (await UsersRequestBuilder.CreateRoleAsync()).Id;
        var model = UsersRequestBuilder.BuildUpdateRoleModel() with
        {
            Name = "",
            Description = new string('x', 301),
            PermissionIds = null,
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        // Act
        var response = await UsersClient.UpdateRole(id, model, token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateRoleModel.Name))
                .ShouldHaveValidationError(nameof(UpdateRoleModel.Description))
                .ShouldHaveValidationError(nameof(UpdateRoleModel.PermissionIds));
    }

    [Test]
    public async Task _204_UpdatesRole_AndVerifiesByFetching()
    {
        // Arrange — create a fresh role, then build an update with new name and description.
        var id = (await UsersRequestBuilder.CreateRoleAsync()).Id;
        var updateModel = UsersRequestBuilder.BuildUpdateRoleModel(
            description: "e2e updated description");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        // Act
        var response = await UsersClient.UpdateRole(id, updateModel, token);

        // Assert — 204 NoContent (no body)
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify round-trip: the role reflects the update.
        var getResponse = await UsersClient.GetRoleById(id, token);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var role = getResponse.Content;
        role.ShouldNotBeNull();
        role!.Id.ShouldBe(id);
        role.Name.ShouldBe(updateModel.Name);
        role.Description.ShouldBe(updateModel.Description);
        role.IsSystem.ShouldBeFalse();
    }
}
