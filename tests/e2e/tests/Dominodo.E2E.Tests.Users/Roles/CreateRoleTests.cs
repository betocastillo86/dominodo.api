using System.Net;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Roles;

/// <summary>
/// Black-box coverage for <c>POST /api/v1/roles</c>, guarded by
/// <c>[HasPermission(Permissions.RolesManage)]</c> on the RolesController.
/// </summary>
[TestFixture]
public sealed class CreateRoleTests : BaseUsersTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildNewRoleModel();

        // Act — no token
        var response = await UsersClient.CreateRole(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksRolesManage()
    {
        // Arrange — bearer for the seeded "Rol Public" user: exists and has a role, but zero permissions.
        var model = UsersRequestBuilder.BuildNewRoleModel();
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await UsersClient.CreateRole(model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenAllRulesViolated()
    {
        // Arrange — break every CreateRoleCommandValidator rule at once:
        // Name NotEmpty, Description MaximumLength(300), Scope must parse to RoleScope, PermissionIds NotNull.
        var model = UsersRequestBuilder.BuildNewRoleModel() with
        {
            Name = "",
            Description = new string('x', 301),
            Scope = "NotAScope",
            PermissionIds = null,
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        // Act
        var response = await UsersClient.CreateRole(model, token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewRoleModel.Name))
                .ShouldHaveValidationError(nameof(NewRoleModel.Description))
                .ShouldHaveValidationError(nameof(NewRoleModel.Scope))
                .ShouldHaveValidationError(nameof(NewRoleModel.PermissionIds));
    }

    [Test]
    public async Task _400_WhenNameExceedsMaxLength()
    {
        // Arrange — Name's MaximumLength(100) rule; can't coexist with the empty-Name case above.
        var model = UsersRequestBuilder.BuildNewRoleModel() with { Name = new string('x', 101) };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        // Act
        var response = await UsersClient.CreateRole(model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewRoleModel.Name));
    }

    [Test]
    public async Task _201_CreatesRole_AndVerifiesByFetching()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildNewRoleModel(description: "e2e description");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        // Act
        var response = await UsersClient.CreateRole(model, token);

        // Assert — 201 with a positive integer id
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = response.Content;
        created.ShouldNotBeNull();
        created!.Id.ShouldBeGreaterThan(0);

        // Verify round-trip: the role is fetchable and its data matches what was sent.
        var getResponse = await UsersClient.GetRoleById(created.Id, token);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var role = getResponse.Content;
        role.ShouldNotBeNull();
        role!.Id.ShouldBe(created.Id);
        role.Name.ShouldBe(model.Name);
        role.Description.ShouldBe(model.Description);
        role.Scope.ShouldBe(model.Scope);
        role.IsSystem.ShouldBeFalse();
        role.Permissions[0].Code.ShouldBe(DominodoConstants.Permission.UsersManage); 
    }
}
