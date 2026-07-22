using Dominodo.Operations.Domain.Visits;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Operations.Domain.Ports;

// All reads are implicitly scoped to the current tenant by the implementation (ForCurrentTenant, doc 09).
public interface IVisitRepository
{
    void Add(Visit visit);
    Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Visit> Items, long TotalCount)> ListAsync(
        PageRequest page,
        VisitStatus? status,
        VisitType? type,
        Guid? apartmentId,
        CancellationToken cancellationToken = default);
}
