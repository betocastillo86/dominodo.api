using Dominodo.Admin.Application.Configuration.CreateSystemSetting;
using Dominodo.Admin.Application.Configuration.GetSystemSettingByKey;
using Dominodo.Admin.Application.Configuration.GetSystemSettings;
using Dominodo.Admin.Application.Configuration.UpdateSystemSetting;
using Dominodo.Admin.Contracts;
using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Admin.Api.Controllers;

// System configuration (domain-model §4.4). Scope follows the X-Tenant header: present resolves the
// tenant override, absent targets the global row. Because Administrador holds settings.* only through a
// tenant membership, the permission resolves only with X-Tenant — so writing a global row requires a
// Platform-role token (e.g. SuperAdmin) with no X-Tenant. No role-name check anywhere (doc 12).
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/system-settings")]
public sealed class SystemSettingsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.SettingsView)]
    [EndpointSummary("Lists global settings plus the current tenant's overrides (when X-Tenant is sent).")]
    [ProducesResponseType(typeof(PagedResult<SystemSettingDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetSystemSettingsQuery(page, pageSize), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{key}", Name = "GetSystemSettingByKey")]
    [HasPermission(Permissions.SettingsView)]
    [EndpointSummary("Gets a setting resolved for the current scope: the tenant override if present, otherwise the global value.")]
    [ProducesResponseType(typeof(SystemSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetByKey(string key, CancellationToken ct)
    {
        var result = await sender.Send(new GetSystemSettingByKeyQuery(key), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.SettingsCreate)]
    [EndpointSummary("Creates a setting. With X-Tenant it creates a tenant override; without it, a global row (Platform-role only).")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Create(CreateSystemSettingRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateSystemSettingCommand(request.Key, request.Value, request.ValueType),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetSystemSettingByKey", new { key = result.Value }, new { key = result.Value })
            : result.ToProblem();
    }

    [HttpPut("{key}")]
    [HasPermission(Permissions.SettingsEdit)]
    [EndpointSummary("Updates a setting's value for the current scope (tenant override with X-Tenant, else the global row).")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> Update(string key, UpdateSystemSettingRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateSystemSettingCommand(key, request.Value, request.ValueType),
            ct);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record CreateSystemSettingRequest(string Key, string Value, string ValueType);

public sealed record UpdateSystemSettingRequest(string Value, string ValueType);
