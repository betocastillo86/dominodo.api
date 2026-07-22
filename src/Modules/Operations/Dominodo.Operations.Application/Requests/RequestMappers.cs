using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Requests;

namespace Dominodo.Operations.Application.Requests;

internal static class RequestMappers
{
    public static RequestDto ToDto(Request r) => new(
        r.Id,
        r.TenantId,
        r.Code,
        r.Type.ToString(),
        r.Category,
        r.Title,
        r.Status.ToString(),
        r.Priority.ToString(),
        r.CreatedByUserId,
        r.ApartmentId,
        r.AssignedToUserId);

    public static RequestDetailDto ToDetailDto(Request r) => new(
        r.Id,
        r.TenantId,
        r.Code,
        r.Type.ToString(),
        r.Category,
        r.Title,
        r.Description,
        r.Location,
        r.Status.ToString(),
        r.Priority.ToString(),
        r.CreatedByUserId,
        r.ApartmentId,
        r.AssignedToUserId,
        r.ResolvedAtUtc,
        r.ClosedAtUtc,
        r.Metadata,
        r.Participants
            .Select(p => new RequestParticipantDto(
                p.Id, p.UserId, p.ParticipantType.ToString(), p.Source.ToString(), p.JoinedAtUtc))
            .ToList(),
        r.Updates
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => new RequestUpdateDto(
                u.Id, u.AuthorUserId, u.Type.ToString(), u.Body, u.IsInternal, u.CreatedAtUtc))
            .ToList(),
        r.StatusHistory
            .OrderBy(h => h.ChangedAtUtc)
            .Select(h => new RequestStatusHistoryDto(
                h.Id, h.FromStatus.ToString(), h.ToStatus.ToString(), h.ChangedByUserId, h.ChangedAtUtc, h.Note))
            .ToList());
}
