using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;

namespace Dominodo.Tenants.Application.Apartments.Residents.GetApartmentResidents;

internal sealed record GetApartmentResidentsQuery(Guid ApartmentId) : IQuery<IReadOnlyList<ResidentDto>>;
