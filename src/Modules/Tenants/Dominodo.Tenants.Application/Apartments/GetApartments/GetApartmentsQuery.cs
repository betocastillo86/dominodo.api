using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Ports;

namespace Dominodo.Tenants.Application.Apartments.GetApartments;

internal sealed record GetApartmentsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Tower = null,
    ApartmentType? Type = null,
    ApartmentStatus? Status = null) : IQuery<PagedResult<ApartmentDto>>;

internal sealed class GetApartmentsQueryHandler(IApartmentRepository apartments)
    : IQueryHandler<GetApartmentsQuery, PagedResult<ApartmentDto>>
{
    public async Task<Result<PagedResult<ApartmentDto>>> Handle(GetApartmentsQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await apartments.ListAsync(page, query.Tower, query.Type, query.Status, ct);

        var dtos = items.Select(ToDto).ToList();
        return new PagedResult<ApartmentDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }

    private static ApartmentDto ToDto(Apartment apartment) => new(
        apartment.Id,
        apartment.TenantId,
        apartment.Tower,
        apartment.Number,
        apartment.Type.ToString(),
        apartment.Status.ToString());
}
