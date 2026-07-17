using Microsoft.AspNetCore.Authorization;

namespace Dominodo.Shared.Infrastructure.Auth;

internal sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
