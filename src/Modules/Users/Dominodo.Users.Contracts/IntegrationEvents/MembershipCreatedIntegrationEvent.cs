namespace Dominodo.Users.Contracts.IntegrationEvents;

// Published when a user is invited into a conjunto (domain-model §1.9). UserId + TenantId let consumers
// (e.g. the host permission-cache invalidator) act without a round-trip.
public sealed record MembershipCreatedIntegrationEvent(Guid MembershipId, Guid UserId, Guid TenantId, int RoleId);
