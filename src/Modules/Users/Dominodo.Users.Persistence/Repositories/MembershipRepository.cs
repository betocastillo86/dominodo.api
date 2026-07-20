using Dominodo.Shared.Infrastructure.Multitenancy;
using Dominodo.Shared.Kernel;
using Dominodo.Users.Domain.Memberships;
using Dominodo.Users.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Users.Persistence.Repositories;

// Tenant-scoped reads funnel through ForCurrentTenant(tenant) so the tenant controller can never touch
// another conjunto's memberships (doc 09). The explicit-id and ListByUser methods query unscoped — they
// are system/cross-tenant facade reads (permission resolution, GetMemberships). The scoping lives here,
// in the persistence adapter; Application stays free of Shared.Infrastructure.
internal sealed class MembershipRepository(UsersDbContext db, ITenantContext tenant) : IMembershipRepository
{
    public void Add(Membership membership) => db.Memberships.Add(membership);

    public void Remove(Membership membership) => db.Memberships.Remove(membership);

    public Task<Membership?> GetByUserAndTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        db.Memberships.FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId, cancellationToken);

    public Task<Membership?> GetActiveByUserAndTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        db.Memberships.FirstOrDefaultAsync(
            m => m.UserId == userId && m.TenantId == tenantId && m.Status == MembershipStatus.Active,
            cancellationToken);

    public async Task<IReadOnlyList<Membership>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.Memberships
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

    public Task<Membership?> GetByIdForCurrentTenantAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Memberships.ForCurrentTenant(tenant).FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Membership>> ListForCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        await db.Memberships
            .ForCurrentTenant(tenant)
            .OrderBy(m => m.InvitedAtUtc)
            .ToListAsync(cancellationToken);
}
