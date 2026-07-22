using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Requests;
using Dominodo.Shared.Infrastructure.Multitenancy;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Operations.Persistence.Repositories;

// Every read is funneled through ForCurrentTenant(tenant) so a caller can never see another tenant's
// requests (doc 09). Application handlers stay free of Shared.Infrastructure — the scoping lives here.
internal sealed class RequestRepository(OperationsDbContext db, ITenantContext tenant) : IRequestRepository
{
    public void Add(Request request) => db.Requests.Add(request);

    public void Remove(Request request) => db.Requests.Remove(request);

    public Task<Request?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Requests
            .ForCurrentTenant(tenant)
            .Include(r => r.Participants)
            .Include(r => r.Updates)
            .Include(r => r.StatusHistory)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Request> Items, long TotalCount)> ListAsync(
        PageRequest page,
        RequestStatus? status,
        RequestType? type,
        RequestPriority? priority,
        Guid? assignedToUserId,
        Guid? apartmentId,
        CancellationToken cancellationToken = default)
    {
        var query = db.Requests.ForCurrentTenant(tenant);

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        if (type is not null)
        {
            query = query.Where(r => r.Type == type);
        }

        if (priority is not null)
        {
            query = query.Where(r => r.Priority == priority);
        }

        if (assignedToUserId is not null)
        {
            query = query.Where(r => r.AssignedToUserId == assignedToUserId);
        }

        if (apartmentId is not null)
        {
            query = query.Where(r => r.ApartmentId == apartmentId);
        }

        var ordered = query.OrderByDescending(r => EF.Property<DateTimeOffset>(r, "CreatedAtUtc"));

        var total = await ordered.LongCountAsync(cancellationToken);
        var items = await ordered
            .Skip(page.Skip)
            .Take(page.Take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    // Explicit-tenant count for the facade (no request-context scoping): "open" = not in a terminal status.
    public Task<int> CountOpenForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        db.Requests.CountAsync(
            r => r.TenantId == tenantId
                && r.Status != RequestStatus.Closed
                && r.Status != RequestStatus.Rejected
                && r.Status != RequestStatus.Cancelled,
            cancellationToken);

    // Explicit-id read for the facade summary (cross-module, no tenant-context scoping).
    public Task<Request?> GetByIdForSummaryAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
}
