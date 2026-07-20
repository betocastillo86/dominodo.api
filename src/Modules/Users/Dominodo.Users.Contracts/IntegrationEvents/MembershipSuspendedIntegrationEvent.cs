namespace Dominodo.Users.Contracts.IntegrationEvents;

// Published when a membership is suspended (domain-model §1.9). The user's tenant permissions must drop
// immediately, so the host evicts their permission-cache entry on this event.
public sealed record MembershipSuspendedIntegrationEvent(Guid MembershipId, Guid UserId, Guid TenantId);
