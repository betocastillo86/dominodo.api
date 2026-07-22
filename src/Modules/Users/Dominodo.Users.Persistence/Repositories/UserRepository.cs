using Dominodo.Shared.Kernel.Pagination;
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

    public async Task<(IReadOnlyList<User> Items, long TotalCount)> ListAsync(
        PageRequest page,
        Guid? tenantId,
        string? name,
        string? email,
        string? phone,
        UserStatus? status,
        string? documentNumber,
        bool? phoneVerified,
        bool? emailVerified,
        CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking();

        if (tenantId is not null)
        {
            // A User is platform-level; the user↔tenant link lives in Membership (same module/DbContext,
            // so no boundary is crossed). Filter = "has at least one membership (any status) in that tenant".
            query = query.Where(u => db.Memberships.Any(m => m.TenantId == tenantId && m.UserId == u.Id));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(u => EF.Functions.Like(u.FirstName + " " + u.LastName, $"%{name}%"));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(u => u.Email != null && EF.Functions.Like(u.Email, $"%{email}%"));
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            query = query.Where(u => EF.Functions.Like(u.Phone, $"%{phone}%"));
        }

        if (status is not null)
        {
            query = query.Where(u => u.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(documentNumber))
        {
            query = query.Where(u => u.DocumentNumber != null && EF.Functions.Like(u.DocumentNumber, $"%{documentNumber}%"));
        }

        if (phoneVerified is not null)
        {
            query = phoneVerified.Value
                ? query.Where(u => u.PhoneVerifiedAtUtc != null)
                : query.Where(u => u.PhoneVerifiedAtUtc == null);
        }

        if (emailVerified is not null)
        {
            query = emailVerified.Value
                ? query.Where(u => u.EmailVerifiedAtUtc != null)
                : query.Where(u => u.EmailVerifiedAtUtc == null);
        }

        var ordered = query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ThenBy(u => u.Id);

        var total = await ordered.LongCountAsync(cancellationToken);
        var items = await ordered
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
