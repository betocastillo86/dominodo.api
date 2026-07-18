using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;

namespace Dominodo.Tenants.Application.Apartments.GetApartmentById;

internal sealed record GetApartmentByIdQuery(Guid ApartmentId) : IQuery<ApartmentDetailDto>;
