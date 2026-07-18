using Dominodo.Shared.Kernel;

namespace Dominodo.Tenants.Domain.Tenants.Events;

public sealed record TenantCreatedDomainEvent(Guid TenantId, string Slug) : IDomainEvent;
