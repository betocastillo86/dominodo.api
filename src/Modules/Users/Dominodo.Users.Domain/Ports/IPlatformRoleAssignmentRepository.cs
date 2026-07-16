using Dominodo.Users.Domain.Roles;

namespace Dominodo.Users.Domain.Ports;

public interface IPlatformRoleAssignmentRepository
{
    Task<IReadOnlyList<PlatformRoleAssignment>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPlatformRoleNamesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(PlatformRoleAssignment assignment);
    Task<bool> ExistsAsync(Guid userId, int roleId, CancellationToken cancellationToken = default);
}
