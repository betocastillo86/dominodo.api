using Dominodo.E2E.Clients.Modules.Operations.Models;

namespace Dominodo.E2E.Clients.Modules.Operations;

/// <summary>
/// Result of <see cref="OperationsRequestBuilder.CreateAnnouncementAsync"/> /
/// <see cref="OperationsRequestBuilder.CreatePublishedAnnouncementAsync"/>: the resolved tenant slug
/// (whether supplied or freshly created) plus the persisted announcement. <see cref="Id"/> is a shortcut
/// for <c>Announcement.Id</c>.
/// </summary>
public sealed record CreatedAnnouncement(string TenantSlug, AnnouncementDetailModel Announcement)
{
    public Guid Id => Announcement.Id;
}
