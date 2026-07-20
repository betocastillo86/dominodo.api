namespace Dominodo.Users.Contracts;

// Public synchronous read surface of the Users module (domain-model §1.7).
public interface IUsersModuleApi
{
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByPhoneAsync(string phoneE164, CancellationToken cancellationToken = default);

    // Returns the union of permissions from the user's Platform-scope role assignments.
    Task<IReadOnlyList<PermissionDto>> GetPlatformPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Returns the user's memberships across all conjuntos (cross-tenant).
    Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Returns the user's effective permissions in a tenant: platform permissions ∪ the permissions of
    // the user's Active membership role in that tenant (domain-model §1.8). Invited/Suspended grant nothing.
    Task<IReadOnlyList<PermissionDto>> GetEffectivePermissionsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
