using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Ports;

namespace Dominodo.Tenants.Application.Apartments.Residents.GetApartmentResidents;

internal sealed class GetApartmentResidentsQueryHandler(IApartmentRepository apartments)
    : IQueryHandler<GetApartmentResidentsQuery, IReadOnlyList<ResidentDto>>
{
    public async Task<Result<IReadOnlyList<ResidentDto>>> Handle(
        GetApartmentResidentsQuery query,
        CancellationToken ct)
    {
        // Scoped by the repository — never returns another tenant's apartment/residents.
        var apartment = await apartments.GetByIdWithResidentsAsync(query.ApartmentId, ct);
        if (apartment is null)
        {
            return Error.NotFound("Apartment.NotFound", "Apartment not found.");
        }

        IReadOnlyList<ResidentDto> residents = apartment.Residents.Select(ToDto).ToList();
        return Result.Success(residents);
    }

    private static ResidentDto ToDto(ApartmentResident r) => new(
        r.Id,
        r.ApartmentId,
        r.UserId,
        r.RelationType.ToString(),
        r.LivesHere,
        r.StartDate,
        r.EndDate,
        r.IsActive);
}
