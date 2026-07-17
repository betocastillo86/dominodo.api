using System.Net;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users;

/// <summary>
/// Locks in that the minted JWT is accepted end to end: the <c>[Authorize]</c> RolesController
/// returns 200 for a minted SuperAdmin token (SuperAdmin bypasses the tenant check) and 401 for
/// an anonymous request. Full roles/permissions coverage is a later slice.
/// </summary>
[TestFixture]
public sealed class RolesJwtSmokeTests : BaseUsersTests
{
    [Test]
    public async Task _200_WithSuperAdminToken()
    {
        // Arrange
        var token = JwtTokenFactory.CreateSuperAdminToken();

        // Act
        var response = await UsersClient.GetRoles(token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act
        var response = await UsersClient.GetRoles();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
