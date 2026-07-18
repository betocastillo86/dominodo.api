using System.Net;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Core.Faker;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Auth;

[TestFixture]
public sealed class LoginUserTests : BaseUsersTests
{
    [Test]
    public async Task _400_WhenPhoneAndPasswordEmpty()
    {
        // Arrange — both required fields intentionally blank (covers the whole LoginCommandValidator)
        var model = new LoginModel { Phone = "", Password = "" };

        // Act
        var response = await UsersClient.Login(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(LoginModel.Phone));
        response.ShouldHaveValidationError(nameof(LoginModel.Password));
    }

    [Test]
    public async Task _401_WhenCredentialsAreInvalid()
    {
        // Arrange — valid E.164 format but no matching user in the DB
        var model = UsersRequestBuilder.BuildLoginModel();

        // Act
        var response = await UsersClient.Login(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.ShouldHaveErrorCode("Auth.InvalidCredentials");
    }

    [Test]
    public async Task _403_WhenAccountNotActive()
    {
        // Arrange — register a user but intentionally skip activation (stays PendingVerification)
        var newUser = UsersRequestBuilder.BuildNewUserModel();
        await UsersRequestBuilder.RegisterUserAsync(newUser);
        var model = new LoginModel { Phone = newUser.Phone, Password = newUser.Password };

        // Act
        var response = await UsersClient.Login(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.ShouldHaveErrorCode("Auth.AccountNotActive");
    }

    [Test]
    public async Task _200_ReturnsTokens_WhenCredentialsAreValid()
    {
        // Arrange — register, activate via SQL (dev-only), then build the login request
        var password = Faker.StrongPassword();
        var user = await UsersRequestBuilder.RegisterUserAsync(password: password);
        var model = new LoginModel { Phone = user.Phone, Password = password };

        // Act
        var response = await UsersClient.Login(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
        response.Content!.AccessToken.ShouldNotBeNullOrWhiteSpace();
        response.Content.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        response.Content.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }
}
