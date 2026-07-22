using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Configuration;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Configuration.GetSystemSettings;

// Lists global settings plus, when an X-Tenant is resolved, that tenant's overrides (domain-model §4.4).
internal sealed record GetSystemSettingsQuery : IQuery<IReadOnlyList<SystemSettingDto>>;

internal sealed class GetSystemSettingsQueryHandler(
    ISystemSettingRepository settings,
    ITenantContext tenant)
    : IQueryHandler<GetSystemSettingsQuery, IReadOnlyList<SystemSettingDto>>
{
    public async Task<Result<IReadOnlyList<SystemSettingDto>>> Handle(GetSystemSettingsQuery query, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        var rows = await settings.GetAllAsync(tenantId, ct);
        return rows.Select(ToDto).ToList();
    }

    private static SystemSettingDto ToDto(SystemSetting s) =>
        new(s.Key, s.TenantId, s.Value, s.ValueType.ToString(), s.UpdatedAtUtc);
}
