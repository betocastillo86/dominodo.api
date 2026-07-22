using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;

namespace Dominodo.Operations.Application.Deliveries.GetDeliveryById;

internal sealed record GetDeliveryByIdQuery(Guid DeliveryId) : IQuery<DeliveryDetailDto>;

internal sealed class GetDeliveryByIdQueryHandler(
    IDeliveryRepository deliveries,
    IResourceAccessAuthorizer authorizer,
    ITenantsModuleApi tenantsModule)
    : IQueryHandler<GetDeliveryByIdQuery, DeliveryDetailDto>
{
    public async Task<Result<DeliveryDetailDto>> Handle(GetDeliveryByIdQuery query, CancellationToken ct)
    {
        // Scoped by the repository — never returns another tenant's delivery.
        var delivery = await deliveries.GetByIdAsync(query.DeliveryId, ct);
        if (delivery is null)
        {
            return Error.NotFound("Delivery.NotFound", "Delivery not found.");
        }

        // Dual-mode: staff hold deliveries.view (any delivery), or the caller is an ACTIVE resident of the
        // destination apartment (sanctioned cross-module read via the Tenants facade). Leak-safe 404 on deny.
        var allowed = await authorizer.HasAccessAsync(
            Permissions.DeliveriesView,
            async (userId, token) =>
            {
                var residents = await tenantsModule.GetApartmentResidentsAsync(delivery.ApartmentId, token);
                return residents.Any(r => r.UserId == userId && r.IsActive);
            },
            ct);
        if (!allowed)
        {
            return Error.NotFound("Delivery.NotFound", "Delivery not found.");
        }

        return DeliveryMappers.ToDetailDto(delivery);
    }
}
