using Dominodo.Operations.Domain.Requests;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Operations.Domain.Ports;

// All reads are implicitly scoped to the current tenant by the implementation (ForCurrentTenant, doc 09),
// so callers never pass a TenantId — cross-tenant leakage is impossible by construction.
public interface IRequestRepository
{
    void Add(Request request);
    void Remove(Request request);

    // Loads the aggregate with its children (participants, updates, status history) for commands and the
    // ownership-aware detail read.
    Task<Request?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Request> Items, long TotalCount)> ListAsync(
        PageRequest page,
        RequestStatus? status,
        RequestType? type,
        RequestPriority? priority,
        Guid? assignedToUserId,
        Guid? apartmentId,
        CancellationToken cancellationToken = default);

    // Facade reads with an EXPLICIT tenant/id (not the request context) — sanctioned cross-module reads.
    Task<int> CountOpenForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Request?> GetByIdForSummaryAsync(Guid id, CancellationToken cancellationToken = default);
}
