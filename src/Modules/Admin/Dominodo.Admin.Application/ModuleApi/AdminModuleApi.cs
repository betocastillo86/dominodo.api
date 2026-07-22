using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Ports;

namespace Dominodo.Admin.Application.ModuleApi;

// Internal implementation of the public Admin facade (domain-model §2.5 / §4.4). Cross-module callers
// depend only on IAdminModuleApi from Contracts. Mirrors TenantsModuleApi: delegates straight to the
// domain ports (no MediatR round-trip for reads).
internal sealed class AdminModuleApi(ISystemSettingRepository settings) : IAdminModuleApi
{
    public async Task<SystemSettingValueDto?> GetSettingAsync(
        string key,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        // Resolution: the tenant override if present, otherwise the global row (domain-model §4.4).
        var setting = await settings.ResolveAsync(key, tenantId, cancellationToken);
        return setting is null ? null : new SystemSettingValueDto(setting.Value, setting.ValueType.ToString());
    }
}
