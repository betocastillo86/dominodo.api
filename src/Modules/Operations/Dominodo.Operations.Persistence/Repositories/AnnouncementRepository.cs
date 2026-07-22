using Dominodo.Operations.Domain.Announcements;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Infrastructure.Multitenancy;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Operations.Persistence.Repositories;

// Every read is funneled through ForCurrentTenant(tenant) so a caller can never see another tenant's
// announcements (doc 09).
internal sealed class AnnouncementRepository(OperationsDbContext db, ITenantContext tenant) : IAnnouncementRepository
{
    public void Add(Announcement announcement) => db.Announcements.Add(announcement);

    public Task<Announcement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Announcements.ForCurrentTenant(tenant).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Announcement> Items, long TotalCount)> ListAsync(
        PageRequest page,
        AnnouncementStatus? status,
        string? category,
        CancellationToken cancellationToken = default)
    {
        var query = db.Announcements.ForCurrentTenant(tenant);

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Category == category);
        }

        var ordered = query
            .OrderBy(a => a.Priority)
            .ThenByDescending(a => a.PublishedAtUtc);

        var total = await ordered.LongCountAsync(cancellationToken);
        var items = await ordered
            .Skip(page.Skip)
            .Take(page.Take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Announcement>> ListActiveAsync(
        string? category,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var query = db.Announcements
            .ForCurrentTenant(tenant)
            .Where(a => a.Status == AnnouncementStatus.Published
                && (a.ExpiresAtUtc == null || a.ExpiresAtUtc > nowUtc));

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Category == category);
        }

        return await query
            .OrderBy(a => a.Priority)
            .ThenByDescending(a => a.PublishedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
