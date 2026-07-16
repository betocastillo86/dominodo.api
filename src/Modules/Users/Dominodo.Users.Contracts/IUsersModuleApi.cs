namespace Dominodo.Users.Contracts;

// Public synchronous read surface of the Users module (domain-model §1.7).
public interface IUsersModuleApi
{
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByPhoneAsync(string phoneE164, CancellationToken cancellationToken = default);

    // Returns the union of permissions from the user's Platform-scope role assignments.
    // GetEffectivePermissions (tenant-scoped) is deferred to the Membership slice.
    Task<IReadOnlyList<PermissionDto>> GetPlatformPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
