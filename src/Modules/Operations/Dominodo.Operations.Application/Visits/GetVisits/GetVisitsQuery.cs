using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Visits;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Operations.Application.Visits.GetVisits;

internal sealed record GetVisitsQuery(
    int Page = 1,
    int PageSize = 20,
    VisitStatus? Status = null,
    VisitType? Type = null,
    Guid? ApartmentId = null) : IQuery<PagedResult<VisitDto>>;

internal sealed class GetVisitsQueryHandler(IVisitRepository visits)
    : IQueryHandler<GetVisitsQuery, PagedResult<VisitDto>>
{
    public async Task<Result<PagedResult<VisitDto>>> Handle(GetVisitsQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await visits.ListAsync(page, query.Status, query.Type, query.ApartmentId, ct);

        var dtos = items.Select(VisitMappers.ToDto).ToList();
        return new PagedResult<VisitDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }
}
