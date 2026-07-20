using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.Memberships.Events;

public sealed record MembershipCreatedDomainEvent(Guid MembershipId, Guid UserId, Guid TenantId, int RoleId) : IDomainEvent;
