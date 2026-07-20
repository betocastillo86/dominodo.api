using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;
using Dominodo.Tenants.Domain.Ports;

namespace Dominodo.Tenants.Application.Apartments.GetApartmentById;

internal sealed record GetApartmentByIdQuery(Guid ApartmentId) : IQuery<ApartmentDetailDto>;

internal sealed class GetApartmentByIdQueryHandler(
    IApartmentRepository apartments,
    IResourceAccessAuthorizer authorizer)
    : IQueryHandler<GetApartmentByIdQuery, ApartmentDetailDto>
{
    public async Task<Result<ApartmentDetailDto>> Handle(GetApartmentByIdQuery query, CancellationToken ct)
    {
        // Scoped by the repository — never returns another tenant's apartment.
        var apartment = await apartments.GetByIdWithResidentsAsync(query.ApartmentId, ct);
        if (apartment is null)
        {
            return Error.NotFound("Apartment.NotFound", "Apartment not found.");
        }

        // Dual-mode: staff hold apartments.view (any apartment), or the caller is an active resident of
        // THIS apartment. Denial returns the same NotFound as a missing row — leak-safe, no existence disclosure.
        var allowed = await authorizer.HasAccessAsync(
            Permissions.ApartmentsView,
            userId => apartment.Residents.Any(r => r.UserId == userId && r.IsActive),
            ct);
        if (!allowed)
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
