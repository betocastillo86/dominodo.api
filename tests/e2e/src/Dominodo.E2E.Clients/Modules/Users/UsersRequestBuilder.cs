using Dominodo.E2E.Clients.Common;
using Dominodo.E2E.Clients.Modules.Users.Models;
using Dominodo.E2E.Core;
using Dominodo.E2E.Core.Faker;
using Dominodo.E2E.Core.Security;

namespace Dominodo.E2E.Clients.Modules.Users;

/// <summary>
/// Builds Users-module request models (fake but valid data by default) and composes full
/// <c>Arrange</c> use cases. Per README §8, any Arrange helper that calls the API throws on
/// non-success — a broken Arrange aborts the test rather than producing a misleading Assert.
/// </summary>
public sealed class UsersRequestBuilder(IUsersClient users, JwtTokenFactory jwtTokenFactory) : BaseRequestBuilder
{
    private readonly IUsersClient _users = users;
    private readonly JwtTokenFactory _jwtTokenFactory = jwtTokenFactory;

    /// <summary>
    /// Builds a valid <see cref="NewUserModel"/> (E.164 phone, unique email, compliant password).
    /// Any field is overridable; use <c>model with { Email = null }</c> to force an absent email.
    /// </summary>
    public NewUserModel BuildNewUserModel(
        string? phone = null,
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        string? password = null)
    {
        return new NewUserModel
        {
            Phone = phone ?? Faker.E164Phone(),
            Email = email ?? $"e2e-{Guid.NewGuid():N}@example.com",
            FirstName = firstName ?? Faker.Name.FirstName(),
            LastName = lastName ?? Faker.Name.LastName(),
            Password = password ?? Faker.StrongPassword(),
        };
    }

    /// <summary>
    /// Full Arrange: registers a user and returns the created id. Throws on non-success.
    /// </summary>
    public async Task<Guid> RegisterUserAsync(NewUserModel? model = null)
    {
        model ??= BuildNewUserModel();

        var response = await _users.Register(model);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: registering a user returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return response.Content!.Id;
    }

    /// <summary>
    /// Builds a valid <see cref="NewRoleModel"/> with a unique name.
    /// Any field is overridable: <c>model with { Name = "Custom" }</c>.
    /// </summary>
    public NewRoleModel BuildNewRoleModel(
        string? name = null,
        string? description = null,
        string? scope = null,
        IReadOnlyList<int>? permissionIds = null)
    {
        return new NewRoleModel
        {
            Name = name ?? $"e2e-{Guid.NewGuid():N}",
            Description = description,
            Scope = scope ?? "Platform",
            PermissionIds = permissionIds ?? [1], // "users.manage"
        };
    }

    /// <summary>
    /// Full Arrange: creates a role using the seeded <c>roles.manage</c> token and returns the
    /// created integer id. Throws on non-success so a broken Arrange aborts the test immediately.
    /// </summary>
    public async Task<int> CreateRoleAsync(NewRoleModel? model = null)
    {
        model ??= BuildNewRoleModel();
        var token = _jwtTokenFactory.GenerateToken(DominodoConstants.Permission.RolesManage);

        var response = await _users.CreateRole(model, token);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Arrange failed: creating a role returned {(int)response.StatusCode}. " +
                $"Body: {response.Error?.Content}");
        }

        return response.Content!.Id;
    }
}
