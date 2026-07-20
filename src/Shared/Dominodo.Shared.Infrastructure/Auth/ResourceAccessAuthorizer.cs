using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;

namespace Dominodo.Shared.Infrastructure.Auth;

internal sealed class ResourceAccessAuthorizer(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IPermissionProvider permissionProvider) : IResourceAccessAuthorizer
{
    public Task<bool> HasAccessAsync(string permission, Func<Guid, bool> isOwner, CancellationToken ct = default) =>
        HasAccessAsync(permission, (userId, _) => Task.FromResult(isOwner(userId)), ct);

    public async Task<bool> HasAccessAsync(
        string permission,
        Func<Guid, CancellationToken, Task<bool>> isOwnerAsync,
        CancellationToken ct = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            return false;
        }

        var userId = currentUser.UserId;
        Guid? tenantId = tenantContext.HasTenant ? tenantContext.TenantId : null;

        var permissions = await permissionProvider.GetEffectivePermissionsAsync(userId, tenantId, ct);
        if (permissions.Contains(permission))
        {
            return true;
        }

        return await isOwnerAsync(userId, ct);
    }
}
