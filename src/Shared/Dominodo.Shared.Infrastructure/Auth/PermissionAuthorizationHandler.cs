using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Dominodo.Shared.Infrastructure.Auth;

// Resolves the caller's effective permissions for the current tenant and checks the requirement.
// Authorization is ALWAYS by permission — no role is hardcoded here. The SuperAdmin role simply
// carries every permission via the seed, so it resolves to a superset that satisfies any check.
// Fails closed. Runs after tenant resolution, so ITenantContext is populated when a tenant
// permission is evaluated.
internal sealed class PermissionAuthorizationHandler(
    IPermissionProvider permissionProvider,
    ITenantContext tenantContext) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userId))
        {
            return; // no usable subject → fail closed
        }

        Guid? tenantId = tenantContext.HasTenant ? tenantContext.TenantId : null;

        var permissions = await permissionProvider.GetEffectivePermissionsAsync(
            userId, tenantId, CancellationToken.None);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
