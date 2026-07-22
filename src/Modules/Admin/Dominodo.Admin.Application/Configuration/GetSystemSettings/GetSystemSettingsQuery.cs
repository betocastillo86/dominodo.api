using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Configuration;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Application.Configuration.GetSystemSettings;

// Lists global settings plus, when an X-Tenant is resolved, that tenant's overrides (domain-model §4.4).
internal sealed record GetSystemSettingsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<SystemSettingDto>>;

internal sealed class GetSystemSettingsQueryHandler(
    ISystemSettingRepository settings,
    ITenantContext tenant)
    : IQueryHandler<GetSystemSettingsQuery, PagedResult<SystemSettingDto>>
{
    public async Task<Result<PagedResult<SystemSettingDto>>> Handle(GetSystemSettingsQuery query, CancellationToken ct)
    {
        Guid? tenantId = tenant.HasTenant ? tenant.TenantId : null;
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await settings.GetAllAsync(tenantId, page, ct);
        var dtos = items.Select(ToDto).ToList();
        return new PagedResult<SystemSettingDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }

    private static SystemSettingDto ToDto(SystemSetting s) =>
        new(s.Key, s.TenantId, s.Value, s.ValueType.ToString(), s.UpdatedAtUtc);
}
