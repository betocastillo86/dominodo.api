using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Ports;

namespace Dominodo.Tenants.Application.Apartments.GetApartmentById;

internal sealed class GetApartmentByIdQueryHandler(IApartmentRepository apartments)
    : IQueryHandler<GetApartmentByIdQuery, ApartmentDetailDto>
{
    public async Task<Result<ApartmentDetailDto>> Handle(GetApartmentByIdQuery query, CancellationToken ct)
    {
        // Scoped by the repository — never returns another tenant's apartment.
        var apartment = await apartments.GetByIdAsync(query.ApartmentId, ct);
        if (apartment is null)
        {
            return Error.NotFound("Apartment.NotFound", "Apartment not found.");
        }

        return new ApartmentDetailDto(
            apartment.Id,
            apartment.TenantId,
            apartment.Tower,
            apartment.Number,
            apartment.Type.ToString(),
            apartment.Status.ToString(),
            apartment.Attributes);
    }
}
