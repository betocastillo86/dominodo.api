using System.Net;
using Dominodo.E2E.Clients.Modules.Admin.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Admin.Devices;

/// <summary>
/// Black-box coverage for <c>POST /api/v1/devices</c> (DevicesController.Register), guarded by plain
/// <c>[Authorize]</c> — self-service by ownership, NOT RBAC: any authenticated user manages only their OWN
/// devices (the registration is keyed to the token's <c>sub</c>, never to a caller-supplied user id).
/// Anonymous ⇒ 401; an invalid body ⇒ 400 Validation.Failed; success is 201 Created ({"id": guid}). The
/// success path is verified against <c>admin.DeviceRegistrations</c> via the dev-only SQL endpoint (the
/// controller exposes no GET), including that a device registered by two different users yields two
/// separate rows — a user only ever touches their own device.
/// </summary>
[TestFixture]
public sealed class RegisterDeviceTests : BaseAdminTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = AdminRequestBuilder.BuildNewDeviceModel();

        // Act — no token
        var response = await AdminClient.RegisterDevice(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _400_WhenPayloadInvalid()
    {
        // Arrange — break every field the validator guards at once: Token is required (NotEmpty) and
        // Platform must parse to the DevicePlatform enum ("Android"/"iOS").
        var model = AdminRequestBuilder.BuildNewDeviceModel() with
        {
            Token = "",
            Platform = "NotAPlatform",
        };
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.RegisterDevice(model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewDeviceModel.Token))
                .ShouldHaveValidationError(nameof(NewDeviceModel.Platform));
    }

    [Test]
    public async Task _400_WhenTokenTooLong()
    {
        // Arrange — the MaximumLength(512) rule on Token; can't coexist with the NotEmpty case above.
        var model = AdminRequestBuilder.BuildNewDeviceModel(deviceToken: new string('x', 513));
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.RegisterDevice(model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewDeviceModel.Token));
    }

    [Test]
    public async Task _201_RegistersDeviceForCurrentUser()
    {
        // Arrange — a real, existing user that carries ZERO permissions: this endpoint needs no permission,
        // only a valid bearer. The device must be linked to THAT user (ownership from the token's sub).
        var deviceToken = $"e2e-device-{Guid.NewGuid():N}";
        var model = AdminRequestBuilder.BuildNewDeviceModel(platform: "iOS", deviceToken: deviceToken);
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.RegisterDevice(model, token);

        // Assert — 201, and the row is persisted linked to the caller (verified via dev SQL).
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content!.Id.ShouldNotBe(Guid.Empty);

        var row = await AdminRequestBuilder.FindDeviceByTokenAsync(deviceToken);
        row.ShouldNotBeNull();
        row!.Id.ShouldBe(response.Content!.Id);
        row.UserId.ShouldBe(DominodoConstants.IntegrationSeed.PublicUserId);
        row.Platform.ShouldBe("iOS");
        row.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task _201_RegisteringSameTokenAsAnotherUserCreatesSeparateOwnDevice()
    {
        // Arrange — user A registers a device token. A device is owned per-user (unique on UserId+Token), so
        // when a DIFFERENT user registers the SAME token they get their OWN separate row: no one reactivates
        // or hijacks another user's device ("solo usuarios propios pueden actualizar su device").
        var deviceToken = $"e2e-device-{Guid.NewGuid():N}";
        var tokenA = JwtTokenFactory.GeneratePublicToken();
        var userA = DominodoConstants.IntegrationSeed.PublicUserId;
        var deviceA = await AdminRequestBuilder.RegisterDeviceAsync(tokenA, deviceToken: deviceToken);

        var tokenB = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.UsersManage);
        var userB = DominodoConstants.IntegrationSeed.UserIdFor(DominodoConstants.Permission.UsersManage);
        var model = AdminRequestBuilder.BuildNewDeviceModel(deviceToken: deviceToken);

        // Act — user B registers the same token
        var response = await AdminClient.RegisterDevice(model, tokenB);

        // Assert — a distinct row owned by user B, leaving user A's untouched.
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content!.Id.ShouldNotBe(deviceA.Id);

        var rowB = await AdminRequestBuilder.FindDeviceByIdAsync(response.Content!.Id);
        rowB.ShouldNotBeNull();
        rowB!.UserId.ShouldBe(userB);

        var rowA = await AdminRequestBuilder.FindDeviceByIdAsync(deviceA.Id);
        rowA.ShouldNotBeNull();
        rowA!.UserId.ShouldBe(userA);
    }
}
