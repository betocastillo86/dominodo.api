using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Dominodo.Shared.Infrastructure.Auth;

// Resolves the caller's effective permissions for the current tenant and checks the requirement.
// SuperAdmin is platform authority (bypass); everything else fails closed. Runs after tenant
// resolution, so ITenantContext is populated when a tenant permission is evaluated.
internal sealed class PermissionAuthorizationHandler(
    IPermissionProvider permissionProvider,
    ITenantContext tenantContext) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // SuperAdmin holds cross-tenant platform authority — not modelled as a single permission.
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

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
