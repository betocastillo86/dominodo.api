using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Tenants.Contracts;

namespace Dominodo.Tenants.Application.Apartments.GetApartments;

internal sealed record GetApartmentsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Tower = null,
    string? Type = null,
    string? Status = null) : IQuery<PagedResult<ApartmentDto>>;
