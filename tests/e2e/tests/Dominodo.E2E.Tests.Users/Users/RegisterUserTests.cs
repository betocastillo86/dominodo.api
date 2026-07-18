using System.Net;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Users;

[TestFixture]
public sealed class RegisterUserTests : BaseUsersTests
{
    [Test]
    public async Task _201_CreatesUser_WithValidData()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildNewUserModel();

        // Act
        var response = await UsersClient.Register(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content.ShouldNotBeNull();
        response.Content!.Id.ShouldNotBe(Guid.Empty);
        response.Headers.Location.ShouldNotBeNull();
    }

    [Test]
    public async Task _201_CreatesUser_WithoutEmail()
    {
        // Arrange — email is optional
        var model = UsersRequestBuilder.BuildNewUserModel() with { Email = null };

        // Act
        var response = await UsersClient.Register(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content!.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task _400_WhenPhoneNotE164()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildNewUserModel() with { Phone = "12345" };

        // Act
        var response = await UsersClient.Register(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewUserModel.Phone));
    }

    [Test]
    public async Task _400_WhenPasswordTooWeak()
    {
        // Arrange — no uppercase/digit, too short
        var model = UsersRequestBuilder.BuildNewUserModel() with { Password = "weak" };

        // Act
        var response = await UsersClient.Register(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewUserModel.Password));
    }

    [Test]
    public async Task _400_WhenFirstNameMissing()
    {
        // Arrange
        var model = UsersRequestBuilder.BuildNewUserModel() with { FirstName = "" };

        // Act
        var response = await UsersClient.Register(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewUserModel.FirstName));
    }

    [Test]
    public async Task _409_WhenPhoneAlreadyRegistered()
    {
        // Arrange — register a user, then reuse its phone
        var first = UsersRequestBuilder.BuildNewUserModel();
        await UsersRequestBuilder.RegisterUserAsync(first);
        var duplicate = UsersRequestBuilder.BuildNewUserModel() with { Phone = first.Phone };

        // Act
        var response = await UsersClient.Register(duplicate);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.ShouldHaveErrorCode("User.PhoneAlreadyRegistered");
    }

    [Test]
    public async Task _409_WhenEmailAlreadyRegistered()
    {
        // Arrange — register a user, then reuse its email (with a fresh phone)
        var first = UsersRequestBuilder.BuildNewUserModel();
        await UsersRequestBuilder.RegisterUserAsync(first);
        var duplicate = UsersRequestBuilder.BuildNewUserModel() with { Email = first.Email };

        // Act
        var response = await UsersClient.Register(duplicate);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.ShouldHaveErrorCode("User.EmailAlreadyRegistered");
    }
}
