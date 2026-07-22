using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;

namespace Dominodo.Operations.Application.Visits.GetVisitById;

internal sealed record GetVisitByIdQuery(Guid VisitId) : IQuery<VisitDetailDto>;

internal sealed class GetVisitByIdQueryHandler(
    IVisitRepository visits,
    IResourceAccessAuthorizer authorizer,
    ITenantsModuleApi tenantsModule)
    : IQueryHandler<GetVisitByIdQuery, VisitDetailDto>
{
    public async Task<Result<VisitDetailDto>> Handle(GetVisitByIdQuery query, CancellationToken ct)
    {
        // Scoped by the repository — never returns another tenant's visit.
        var visit = await visits.GetByIdAsync(query.VisitId, ct);
        if (visit is null)
        {
            return Error.NotFound("Visit.NotFound", "Visit not found.");
        }

        // Dual-mode: staff hold visits.view (any visit), or the caller is an ACTIVE resident of the
        // destination apartment (sanctioned cross-module read via the Tenants facade). Leak-safe 404 on deny.
        var allowed = await authorizer.HasAccessAsync(
            Permissions.VisitsView,
            async (userId, token) =>
            {
                var residents = await tenantsModule.GetApartmentResidentsAsync(visit.ApartmentId, token);
                return residents.Any(r => r.UserId == userId && r.IsActive);
            },
            ct);
        if (!allowed)
        {
            return Error.NotFound("Visit.NotFound", "Visit not found.");
        }

        return VisitMappers.ToDetailDto(visit);
    }
}
