using System.Net;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Users.Auth;

[TestFixture]
public sealed class ConfirmOtpTests : BaseUsersTests
{
    [Test]
    public async Task _204_ActivatesUser_WhenCodeIsValid()
    {
        // Arrange — a self-registered user still pending verification, plus a known OTP seeded for its phone
        var user = await UsersRequestBuilder.RegisterUserAsync(activate: false);
        user.Status.ShouldBe("PendingVerification"); // precondition: not yet active
        var code = await UsersRequestBuilder.IssueOtpAsync(user.Phone);
        var model = UsersRequestBuilder.BuildConfirmOtpModel(phone: user.Phone, code: code);

        // Act — anonymous endpoint (no token)
        var response = await UsersClient.ConfirmOtp(model);

        // Assert — 204, and the effect really happened: the user is now Active with a verified phone
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var persisted = await UsersRequestBuilder.GetUserAsync(user.Id);
        persisted.Status.ShouldBe("Active");
        persisted.PhoneVerified.ShouldBeTrue();
    }

    [Test]
    public async Task _400_WhenPhoneAndCodeEmpty()
    {
        // Arrange — both required fields blank (covers the whole VerifyPhoneCommandValidator)
        var model = new ConfirmOtpModel("", "");

        // Act
        var response = await UsersClient.ConfirmOtp(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(ConfirmOtpModel.Phone));
        response.ShouldHaveValidationError(nameof(ConfirmOtpModel.Code));
    }

    [Test]
    public async Task _400_WhenCodeIsIncorrect()
    {
        // Arrange — a real active OTP is seeded, but we confirm with the wrong code
        var user = await UsersRequestBuilder.RegisterUserAsync(activate: false);
        await UsersRequestBuilder.IssueOtpAsync(user.Phone);
        var model = UsersRequestBuilder.BuildConfirmOtpModel(phone: user.Phone, code: "999999");

        // Act
        var response = await UsersClient.ConfirmOtp(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Otp.Invalid");
    }

    [Test]
    public async Task _404_WhenNoActiveCodeForPhone()
    {
        // Arrange — well-formed phone with no OTP ever issued for it
        var model = UsersRequestBuilder.BuildConfirmOtpModel();

        // Act
        var response = await UsersClient.ConfirmOtp(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("Otp.NotFound");
    }
}
