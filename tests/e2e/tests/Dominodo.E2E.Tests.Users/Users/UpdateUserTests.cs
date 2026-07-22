using System.Net;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Users;

/// <summary>
/// Black-box coverage for <c>PUT /api/v1/users/{id}</c> (UsersController.Update), guarded by
/// <c>[HasPermission(Permissions.UsersEdit)]</c>. Editing a user is a <b>platform</b> capability: the
/// endpoint is tenant-agnostic (no <c>X-Tenant</c>), so only a caller whose <c>users.edit</c> comes from a
/// Platform grant is authorized — a grant held only through a tenant membership does not resolve here.
/// Success is 204 NoContent (the update is confirmed by a follow-up GET).
/// </summary>
[TestFixture]
public sealed class UpdateUserTests : BaseUsersTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildUpdateUserModel();

        // Act — no token
        var response = await UsersClient.UpdateUser(Guid.NewGuid(), model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksUsersEdit()
    {
        // Arrange — the seeded "Rol Public" user: a real, existing user on a Platform role with ZERO
        // permissions. Only a caller holding users.edit at platform level may edit.
        var model = UsersRequestBuilder.BuildUpdateUserModel();
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await UsersClient.UpdateUser(Guid.NewGuid(), model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _403_WhenUserHasUsersEditOnlyViaTenant()
    {
        // Arrange — the seeded tenant user holds users.edit ONLY via an Active membership (a Tenant-scope
        // role) in the seeded tenant. This platform endpoint resolves no tenant (no X-Tenant), so the
        // caller's effective permissions collapse to its platform grants (none) ⇒ authorization fails
        // closed. A tenant grant cannot authorize the platform edit-user capability.
        var model = UsersRequestBuilder.BuildUpdateUserModel();
        var token = JwtTokenFactory.GenerateTenantToken(DominodoConstants.Permission.UsersEdit);

        // Act
        var response = await UsersClient.UpdateUser(Guid.NewGuid(), model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenAllValidationRulesViolated()
    {
        // Arrange — break every UpdateUserCommandValidator rule at once: FirstName NotEmpty,
        // LastName MaximumLength(100), Email EmailAddress, PreferredLanguage MaximumLength(10).
        var target = await UsersRequestBuilder.RegisterUserAsync(activate: false);
        var model = UsersRequestBuilder.BuildUpdateUserModel() with
        {
            FirstName = "",
            LastName = new string('x', 101),
            Email = "not-an-email",
            PreferredLanguage = new string('x', 11),
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.UsersEdit);

        // Act
        var response = await UsersClient.UpdateUser(target.Id, model, token);

        // Assert — one validation failure carrying an error per broken field.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(UpdateUserModel.FirstName));
        response.ShouldHaveValidationError(nameof(UpdateUserModel.LastName));
        response.ShouldHaveValidationError(nameof(UpdateUserModel.Email));
        response.ShouldHaveValidationError(nameof(UpdateUserModel.PreferredLanguage));
    }

    [Test]
    public async Task _404_WhenUserDoesNotExist()
    {
        // Arrange — valid payload, authorized caller, but an id that maps to no user.
        var model = UsersRequestBuilder.BuildUpdateUserModel();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.UsersEdit);

        // Act
        var response = await UsersClient.UpdateUser(Guid.NewGuid(), model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("User.NotFound");
    }

    [Test]
    public async Task _204_UpdatesUser_AndVerifiesByFetching()
    {
        // Arrange — register a fresh user, then build an update with new profile values.
        var target = await UsersRequestBuilder.RegisterUserAsync(activate: false);
        var update = UsersRequestBuilder.BuildUpdateUserModel(
            firstName: "UpdatedFirst",
            lastName: "UpdatedLast",
            preferredLanguage: "en");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.UsersEdit);

        // Act
        var response = await UsersClient.UpdateUser(target.Id, update, token);

        // Assert — 204 NoContent (no body).
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify round-trip: the persisted user reflects the update.
        var getResponse = await UsersClient.GetById(target.Id);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = getResponse.Content;
        user.ShouldNotBeNull();
        user!.Id.ShouldBe(target.Id);
        user.FirstName.ShouldBe(update.FirstName);
        user.LastName.ShouldBe(update.LastName);
        user.Email.ShouldBe(update.Email);
    }
}
