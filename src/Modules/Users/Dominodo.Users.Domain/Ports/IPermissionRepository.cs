using Dominodo.Users.Domain.Roles;

namespace Dominodo.Users.Domain.Ports;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetByRoleIdsAsync(IEnumerable<int> roleIds, CancellationToken cancellationToken = default);
}
