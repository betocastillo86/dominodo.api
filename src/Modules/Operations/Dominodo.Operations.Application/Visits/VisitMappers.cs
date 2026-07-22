using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Visits;

namespace Dominodo.Operations.Application.Visits;

internal static class VisitMappers
{
    public static VisitDto ToDto(Visit v) => new(
        v.Id,
        v.TenantId,
        v.ApartmentId,
        v.Type.ToString(),
        v.Status.ToString(),
        v.VisitorName,
        v.RegisteredByUserId,
        v.EntryAtUtc,
        v.ExitAtUtc);

    public static VisitDetailDto ToDetailDto(Visit v) => new(
        v.Id,
        v.TenantId,
        v.ApartmentId,
        v.Type.ToString(),
        v.Status.ToString(),
        v.VisitorName,
        v.VisitorDocument,
        v.PhotoUrl,
        v.VehiclePlate,
        v.AmountPaid,
        v.RegisteredByUserId,
        v.AuthorizedByUserId,
        v.EntryAtUtc,
        v.ExitAtUtc,
        v.Metadata);
}
