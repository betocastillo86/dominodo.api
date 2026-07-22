using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Configuration;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Configuration.GetSystemSettingByKey;

// Reads a single setting resolved against the current scope: the tenant override if an X-Tenant is
// present and one exists, otherwise the global row (domain-model §4.4).
internal sealed record GetSystemSettingByKeyQuery(string Key) : IQuery<SystemSettingDto>;

internal sealed class GetSystemSettingByKeyQueryHandler(
    ISystemSettingRepository settings,
    ITenantContext tenant)
    : IQueryHandler<GetSystemSettingByKeyQuery, SystemSettingDto>
{
    public async Task<Result<SystemSettingDto>> Handle(GetSystemSettingByKeyQuery query, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        var setting = await settings.ResolveAsync(query.Key, tenantId, ct);

        if (setting is null)
        {
            return Error.NotFound("SystemSetting.NotFound", $"No setting found for key '{query.Key}'.");
        }

        return new SystemSettingDto(setting.Key, setting.TenantId, setting.Value, setting.ValueType.ToString(), setting.UpdatedAtUtc);
    }
}
