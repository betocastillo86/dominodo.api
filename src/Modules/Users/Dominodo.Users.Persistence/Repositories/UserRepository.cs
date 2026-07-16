using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Users.Persistence.Repositories;

internal sealed class UserRepository(UsersDbContext db) : IUserRepository
{
    public void Add(User user) => db.Users.Add(user);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);

    public Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(u => u.Phone == phone, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(u => u.Email == email, cancellationToken);
}
