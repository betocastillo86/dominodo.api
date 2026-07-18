using Dominodo.Shared.Kernel;

namespace Dominodo.Tenants.Domain.Tenants.Events;

// Raised on every status transition. The directory cache (slug → Id) is evicted in response so a
// suspended tenant stops resolving immediately instead of lingering until the TTL expires.
public sealed record TenantStatusChangedDomainEvent(Guid TenantId, string Slug, string Status) : IDomainEvent;
