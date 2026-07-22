using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Deliveries;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Operations.Application.Deliveries.GetDeliveries;

internal sealed record GetDeliveriesQuery(
    int Page = 1,
    int PageSize = 20,
    DeliveryStatus? Status = null,
    DeliveryType? Type = null,
    Guid? ApartmentId = null) : IQuery<PagedResult<DeliveryDto>>;

internal sealed class GetDeliveriesQueryHandler(IDeliveryRepository deliveries)
    : IQueryHandler<GetDeliveriesQuery, PagedResult<DeliveryDto>>
{
    public async Task<Result<PagedResult<DeliveryDto>>> Handle(GetDeliveriesQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await deliveries.ListAsync(page, query.Status, query.Type, query.ApartmentId, ct);

        var dtos = items.Select(DeliveryMappers.ToDto).ToList();
        return new PagedResult<DeliveryDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }
}
