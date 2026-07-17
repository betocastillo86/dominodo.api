using Microsoft.AspNetCore.Authorization;

namespace Dominodo.Shared.Infrastructure.Auth;

// Authorize an endpoint by a permission code, e.g. [HasPermission(Permissions.RolesManage)].
// The code is encoded as the policy name ("perm:<code>"); PermissionPolicyProvider turns it into a
// PermissionRequirement on the fly, so there is no per-permission policy to register.
// See docs/architecture/12-permission-authorization.md.
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public HasPermissionAttribute(string permission)
        : base(PolicyPrefix + permission)
    {
    }
}
