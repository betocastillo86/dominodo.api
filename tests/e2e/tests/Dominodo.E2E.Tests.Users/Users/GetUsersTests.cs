using System.Net;
using Dominodo.E2E.Core;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Users;

/// <summary>
/// Black-box coverage for <c>GET /api/v1/users</c> (admin listing), guarded by
/// <c>[HasPermission(Permissions.UsersView)]</c> on the UsersController. Permissions resolve
/// server-side from the DB by the token's subject: an anonymous request is 401, a real user
/// lacking <c>users.view</c> is 403, and the seeded <c>users.view</c> Platform user is authorized.
/// The listing has no FluentValidation validator and page/pageSize are clamped, so the only 400
/// is a model-binding failure (an unparseable <c>status</c> filter).
/// </summary>
[TestFixture]
public sealed class GetUsersTests : BaseUsersTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await UsersClient.GetUsers();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksUsersView()
    {
        // Arrange — bearer for the seeded "Rol Public" user: real, but zero permissions.
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await UsersClient.GetUsers(token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenStatusFilterInvalid()
    {
        // Arrange — authorized caller; the only reachable 400 is an unparseable enum filter
        // (there is no validator, and pagination is clamped rather than rejected).
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.UsersView);

        // Act
        var response = await UsersClient.GetUsers(status: "NotAStatus", token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task _200_ReturnsUsersMatchingFilter()
    {
        // Arrange — a fresh user (isolated by its unique phone) + the seeded users.view Platform token.
        var user = await UsersRequestBuilder.RegisterUserAsync();
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.UsersView);

        // Act — filter by the just-registered phone so the assertion is self-contained on shared data.
        var response = await UsersClient.GetUsers(phone: user.Phone, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var paged = response.Content;
        paged.ShouldNotBeNull();

        var listed = paged!.Items.ShouldHaveSingleItem();
        listed.Id.ShouldBe(user.Id);
        listed.Phone.ShouldBe(user.Phone);
        listed.FirstName.ShouldBe(user.FirstName);
        listed.LastName.ShouldBe(user.LastName);
    }
}
