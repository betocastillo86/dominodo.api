namespace Dominodo.Admin.Contracts;

// Public synchronous read surface of the Admin module (domain-model §2.5 / §4.4). Other modules depend
// only on this interface from Contracts. GetSettingAsync resolves the tenant override if present,
// otherwise the global value.
public interface IAdminModuleApi
{
    Task<SystemSettingValueDto?> GetSettingAsync(
        string key,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
