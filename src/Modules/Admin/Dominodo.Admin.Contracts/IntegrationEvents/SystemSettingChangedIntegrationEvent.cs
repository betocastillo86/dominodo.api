namespace Dominodo.Admin.Contracts.IntegrationEvents;

// Published when a SystemSetting is created or updated. The host consumes it to evict its in-memory
// settings cache so ISystemSettings reads reflect the change immediately (domain-model §4.4).
public sealed record SystemSettingChangedIntegrationEvent(string Key, Guid? TenantId);
