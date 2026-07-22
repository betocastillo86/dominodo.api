using Dominodo.Shared.Kernel;

namespace Dominodo.Admin.Domain.Configuration.Events;

// Raised whenever a SystemSetting's value is created or updated. Bridged to the public
// SystemSettingChangedIntegrationEvent (over the outbox) so the host can evict its settings cache.
public sealed record SystemSettingChangedDomainEvent(string Key, Guid? TenantId) : IDomainEvent;
