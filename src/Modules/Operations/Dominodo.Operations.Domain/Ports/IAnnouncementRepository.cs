using Dominodo.Operations.Domain.Announcements;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Operations.Domain.Ports;

// All reads are implicitly scoped to the current tenant by the implementation (ForCurrentTenant, doc 09).
public interface IAnnouncementRepository
{
    void Add(Announcement announcement);
    Task<Announcement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Admin listing — includes drafts; optional status/category filters.
    Task<(IReadOnlyList<Announcement> Items, long TotalCount)> ListAsync(
        PageRequest page,
        AnnouncementStatus? status,
        string? category,
        CancellationToken cancellationToken = default);

    // Member-facing /mine feed: only ACTIVE announcements (Published + not expired), optional category
    // filter, ordered by Priority asc then PublishedAtUtc desc. Audience narrowing happens in the handler.
    Task<IReadOnlyList<Announcement>> ListActiveAsync(
        string? category,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}
