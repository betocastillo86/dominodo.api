using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Deliveries;

namespace Dominodo.Operations.Application.Deliveries;

internal static class DeliveryMappers
{
    public static DeliveryDto ToDto(Delivery d) => new(
        d.Id,
        d.TenantId,
        d.Code,
        d.ApartmentId,
        d.Type.ToString(),
        d.Status.ToString(),
        d.RegisteredByUserId,
        d.Carrier,
        d.ReceivedAtUtc,
        d.DeliveredAtUtc);

    public static DeliveryDetailDto ToDetailDto(Delivery d) => new(
        d.Id,
        d.TenantId,
        d.Code,
        d.ApartmentId,
        d.Type.ToString(),
        d.Status.ToString(),
        d.RegisteredByUserId,
        d.Carrier,
        d.Comment,
        d.PhotoUrl,
        d.ReceivedAtUtc,
        d.DeliveredAtUtc,
        d.ReceivedByName,
        d.DeliveredToUserId,
        d.Metadata);
}
