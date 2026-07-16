using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Domain.Roles;

namespace Dominodo.Users.Domain.Ports;

public interface IRoleRepository
{
    void Add(Role role);
    Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int excludeRoleId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Role> Items, long TotalCount)> ListAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<int> GetMaxIdAsync(CancellationToken cancellationToken = default);
}
