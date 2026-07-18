using System.Net;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Users;

[TestFixture]
public sealed class GetUserByIdTests : BaseUsersTests
{
    [Test]
    public async Task _200_ReturnsUser_AfterRegistration()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildNewUserModel();
        var id = (await UsersRequestBuilder.RegisterUserAsync(model)).Id;

        // Act
        var response = await UsersClient.GetById(id);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = response.Content;
        user.ShouldNotBeNull();
        user!.Id.ShouldBe(id);
        user.Phone.ShouldBe(model.Phone);
        user.Email.ShouldBe(model.Email);
        user.FirstName.ShouldBe(model.FirstName);
        user.LastName.ShouldBe(model.LastName);
        user.Status.ShouldBe("PendingVerification");
        user.PhoneVerified.ShouldBeFalse();
    }

    [Test]
    public async Task _404_WhenUserDoesNotExist()
    {
        // Act
        var response = await UsersClient.GetById(Guid.NewGuid());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("User.NotFound");
    }
}
