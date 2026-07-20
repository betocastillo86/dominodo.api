using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.Memberships.Events;

public sealed record MembershipSuspendedDomainEvent(Guid MembershipId, Guid UserId, Guid TenantId) : IDomainEvent;
