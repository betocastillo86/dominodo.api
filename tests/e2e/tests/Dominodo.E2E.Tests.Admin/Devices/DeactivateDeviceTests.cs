using System.Net;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Admin.Devices;

/// <summary>
/// Black-box coverage for <c>DELETE /api/v1/devices/{id}</c> (DevicesController.Deactivate), guarded by
/// plain <c>[Authorize]</c> — self-service by ownership: a user may deactivate only their OWN device.
/// Anonymous ⇒ 401; success is 204 NoContent (verified deactivated via the dev-only SQL endpoint, since the
/// controller exposes no GET). A device that does not exist OR belongs to another user is a leak-safe 404
/// DeviceRegistration.NotFound — the latter is how "solo usuarios propios pueden actualizar su device" is
/// enforced.
///
/// Note: there is no reachable 400 here — the route is <c>{id:guid}</c> with no body/validator, so a
/// non-guid segment fails the route constraint (404), never 400. The "los mismos" 400 slot is therefore
/// covered by the ownership/unknown 404 cases below.
/// </summary>
[TestFixture]
public sealed class DeactivateDeviceTests : BaseAdminTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Act — no token
        var response = await AdminClient.DeactivateDevice(Guid.NewGuid());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _404_WhenDeviceDoesNotExist()
    {
        // Arrange — a valid bearer but an id that matches no device.
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.DeactivateDevice(Guid.NewGuid(), token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("DeviceRegistration.NotFound");
    }

    [Test]
    public async Task _404_WhenDeactivatingAnotherUsersDevice()
    {
        // Arrange — user A registers a device; user B tries to deactivate it. Ownership is enforced in the
        // handler, so B sees a leak-safe 404 and A's device stays active.
        var tokenA = JwtTokenFactory.GeneratePublicToken();
        var deviceA = await AdminRequestBuilder.RegisterDeviceAsync(tokenA);

        var tokenB = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.UsersManage);

        // Act
        var response = await AdminClient.DeactivateDevice(deviceA.Id, tokenB);

        // Assert — 404 for B, and A's device remains active (untouched).
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.ShouldHaveErrorCode("DeviceRegistration.NotFound");

        var row = await AdminRequestBuilder.FindDeviceByIdAsync(deviceA.Id);
        row.ShouldNotBeNull();
        row!.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task _204_DeactivatesOwnDevice()
    {
        // Arrange — a zero-permission user registers a device (endpoint needs no permission, only a bearer),
        // then deactivates its OWN device.
        var token = JwtTokenFactory.GeneratePublicToken();
        var device = await AdminRequestBuilder.RegisterDeviceAsync(token);

        // Act
        var response = await AdminClient.DeactivateDevice(device.Id, token);

        // Assert — 204, and the row is flipped to inactive (verified via dev SQL).
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var row = await AdminRequestBuilder.FindDeviceByIdAsync(device.Id);
        row.ShouldNotBeNull();
        row!.IsActive.ShouldBeFalse();
    }
}
