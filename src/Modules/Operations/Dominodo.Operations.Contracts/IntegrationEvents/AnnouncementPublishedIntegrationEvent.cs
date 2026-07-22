namespace Dominodo.Operations.Contracts.IntegrationEvents;

public sealed record AnnouncementPublishedIntegrationEvent(
    Guid AnnouncementId,
    Guid TenantId,
    Guid PublishedByUserId,
    DateTimeOffset PublishedAtUtc);
