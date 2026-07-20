namespace Dominodo.Users.Contracts.IntegrationEvents;

// Published when a membership's role changes or it is reactivated (domain-model §1.9). Feeds permission-
// cache eviction so a role change/reactivation takes effect within ~1-2s instead of waiting for the TTL.
public sealed record MembershipChangedIntegrationEvent(Guid MembershipId, Guid UserId, Guid TenantId, int RoleId);
