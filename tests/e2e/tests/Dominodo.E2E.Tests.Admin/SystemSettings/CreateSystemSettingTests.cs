using System.Net;
using Dominodo.E2E.Clients.Modules.Admin.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Tests.Shared.Assertions;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Admin.SystemSettings;

/// <summary>
/// Black-box coverage for <c>POST /api/v1/system-settings</c> (SystemSettingsController.Create), guarded by
/// <c>[HasPermission(Permissions.SettingsCreate)]</c>. With no X-Tenant it writes the GLOBAL row, which only
/// a Platform-scope <c>settings.create</c> token can do. Anonymous ⇒ 401; a bearer lacking the permission ⇒
/// 403; an invalid body ⇒ 400 Validation.Failed; a duplicate (Key, scope) ⇒ 409 SystemSetting.AlreadyExists;
/// success is 201 Created and the row is then observable via GET /{key}.
/// </summary>
[TestFixture]
public sealed class CreateSystemSettingTests : BaseAdminTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        // Arrange
        var model = AdminRequestBuilder.BuildNewSystemSettingModel();

        // Act — no token
        var response = await AdminClient.CreateSystemSetting(model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _403_WhenUserLacksSettingsCreate()
    {
        // Arrange — a real, existing user with ZERO permissions. Proves only a settings.create holder may
        // create a setting (the "solo platform con create setting" scenario).
        var model = AdminRequestBuilder.BuildNewSystemSettingModel();
        var token = JwtTokenFactory.GeneratePublicToken();

        // Act
        var response = await AdminClient.CreateSystemSetting(model, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task _400_WhenBodyIsInvalid()
    {
        // Arrange — break the FluentValidation rules evaluated once the body binds: Key empty, Value null.
        // (ValueType is an enum, rejected at JSON binding before the validator runs — covered separately.)
        var model = AdminRequestBuilder.BuildNewSystemSettingModel() with
        {
            Key = null,
            Value = null,
        };
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsCreate);

        // Act
        var response = await AdminClient.CreateSystemSetting(model, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewSystemSettingModel.Key))
                .ShouldHaveValidationError(nameof(NewSystemSettingModel.Value));
    }

    [Test]
    public async Task _400_WhenValueTypeIsNotAValidEnum()
    {
        // Arrange — an unknown enum name fails at JSON binding (JsonStringEnumConverter); the
        // InvalidModelStateResponseFactory maps it to the same Validation.Failed shape as a validator error.
        var model = AdminRequestBuilder.BuildNewSystemSettingModel(valueType: "NotAType");
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsCreate);

        // Act
        var response = await AdminClient.CreateSystemSetting(model, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewSystemSettingModel.ValueType));
    }

    [Test]
    public async Task _400_WhenKeyExceedsMaxLength()
    {
        // Arrange — the one Key rule that can't coexist with the empty-Key case above: MaximumLength(200).
        var model = AdminRequestBuilder.BuildNewSystemSettingModel(key: new string('k', 201));
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsCreate);

        // Act
        var response = await AdminClient.CreateSystemSetting(model, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveValidationError(nameof(NewSystemSettingModel.Key));
    }

    [Test]
    public async Task _409_WhenSettingAlreadyExists()
    {
        // Arrange — a setting that already exists in the global scope. (The prompt called this "400 repeated
        // setting"; the API returns 409 Conflict — Error.Conflict("SystemSetting.AlreadyExists").)
        var existing = await AdminRequestBuilder.CreateSystemSettingAsync();
        var duplicate = AdminRequestBuilder.BuildNewSystemSettingModel(key: existing.Key);
        var token = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsCreate);

        // Act
        var response = await AdminClient.CreateSystemSetting(duplicate, token: token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.ShouldHaveErrorCode("SystemSetting.AlreadyExists");
    }

    [Test]
    public async Task _201_CreatesSetting_AndVerifiesByFetching()
    {
        // Arrange — a fresh, unique setting. (The prompt called this "200 verificar guardado"; the API
        // returns 201 Created, and the saved value is verified via GET /{key}.)
        var model = AdminRequestBuilder.BuildNewSystemSettingModel(value: "e2e-saved-value", valueType: "String");
        var createToken = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsCreate);

        // Act
        var response = await AdminClient.CreateSystemSetting(model, token: createToken);

        // Assert — 201 Created
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify round-trip: the global row is persisted with the sent value.
        var viewToken = JwtTokenFactory.GenerateToken(DominodoConstants.Permission.SettingsView);
        var getResponse = await AdminClient.GetSystemSettingByKey(model.Key!, token: viewToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var saved = getResponse.Content;
        saved.ShouldNotBeNull();
        saved!.Key.ShouldBe(model.Key);
        saved.TenantId.ShouldBeNull();
        saved.Value.ShouldBe("e2e-saved-value");
        saved.ValueType.ShouldBe("String");
    }
}
