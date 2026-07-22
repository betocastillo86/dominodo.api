using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Announcements;

namespace Dominodo.Operations.Application.Announcements;

internal static class AnnouncementMappers
{
    public static AnnouncementDto ToDto(Announcement a) => new(
        a.Id,
        a.TenantId,
        a.Title,
        a.Category,
        a.Priority,
        a.Status.ToString(),
        a.AudienceType.ToString(),
        a.PublishedAtUtc,
        a.ExpiresAtUtc);

    public static AnnouncementDetailDto ToDetailDto(Announcement a) => new(
        a.Id,
        a.TenantId,
        a.Title,
        a.Body,
        a.Category,
        a.Priority,
        a.Status.ToString(),
        a.AudienceType.ToString(),
        a.AudienceFilter,
        a.PublishedAtUtc,
        a.ExpiresAtUtc,
        a.PublishedByUserId);
}
