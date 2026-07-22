using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Announcements.Events;

public sealed record AnnouncementPublishedDomainEvent(
    Guid AnnouncementId,
    Guid TenantId,
    Guid PublishedByUserId,
    DateTimeOffset PublishedAtUtc) : IDomainEvent;
