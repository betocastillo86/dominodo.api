using System.Net;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Auth;

[TestFixture]
public sealed class RequestOtpTests : BaseUsersTests
{
    [Test]
    public async Task _202_WhenPhoneBelongsToRegisteredUser()
    {
        // Arrange — a self-registered ("public") user whose phone is not yet verified
        var user = await UsersRequestBuilder.RegisterUserAsync(activate: false);
        var model = UsersRequestBuilder.BuildRequestOtpModel(phone: user.Phone);

        // Act — anonymous endpoint (no token)
        var response = await UsersClient.RequestOtp(model);

        // Assert — the handler queues the OTP and returns 202 Accepted (empty body)
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Test]
    public async Task _400_WhenPhoneIsInvalid()
    {
        // Arrange — empty phone breaks both rules of RequestPhoneVerificationCommandValidator
        // (.NotEmpty() and .Matches(E.164)); covers the whole validator.
        var model = UsersRequestBuilder.BuildRequestOtpModel() with { Phone = "" };

        // Act
        var response = await UsersClient.RequestOtp(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(RequestOtpModel.Phone));
    }

    [Test]
    public async Task _404_WhenNoUserHasThatPhone()
    {
        // Arrange — well-formed E.164 phone that belongs to no registered user
        var model = UsersRequestBuilder.BuildRequestOtpModel();

        // Act
        var response = await UsersClient.RequestOtp(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("User.NotFound");
    }
}
