using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Ports;

namespace Dominodo.Tenants.Application.Apartments.GetApartments;

internal sealed class GetApartmentsQueryHandler(IApartmentRepository apartments)
    : IQueryHandler<GetApartmentsQuery, PagedResult<ApartmentDto>>
{
    public async Task<Result<PagedResult<ApartmentDto>>> Handle(GetApartmentsQuery query, CancellationToken ct)
    {
        // Optional filters are validated at the boundary; parse leniently to null when absent/blank.
        ApartmentType? type = Enum.TryParse<ApartmentType>(query.Type, out var t) ? t : null;
        ApartmentStatus? status = Enum.TryParse<ApartmentStatus>(query.Status, out var s) ? s : null;

        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await apartments.ListAsync(page, query.Tower, type, status, ct);

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
