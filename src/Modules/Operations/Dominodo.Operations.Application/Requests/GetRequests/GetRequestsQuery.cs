using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Requests;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Operations.Application.Requests.GetRequests;

internal sealed record GetRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    RequestStatus? Status = null,
    RequestType? Type = null,
    RequestPriority? Priority = null,
    Guid? AssignedToUserId = null,
    Guid? ApartmentId = null) : IQuery<PagedResult<RequestDto>>;

internal sealed class GetRequestsQueryHandler(IRequestRepository requests)
    : IQueryHandler<GetRequestsQuery, PagedResult<RequestDto>>
{
    public async Task<Result<PagedResult<RequestDto>>> Handle(GetRequestsQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await requests.ListAsync(
            page, query.Status, query.Type, query.Priority, query.AssignedToUserId, query.ApartmentId, ct);

        var dtos = items.Select(RequestMappers.ToDto).ToList();
        return new PagedResult<RequestDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }
}
