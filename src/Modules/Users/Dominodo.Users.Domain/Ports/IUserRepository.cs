using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Domain.Users;

namespace Dominodo.Users.Domain.Ports;

public interface IUserRepository
{
    void Add(User user);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<User> Items, long TotalCount)> ListAsync(
        PageRequest page,
        Guid? tenantId,
        string? name,
        string? email,
        string? phone,
        UserStatus? status,
        string? documentNumber,
        bool? phoneVerified,
        bool? emailVerified,
        CancellationToken cancellationToken = default);
}
