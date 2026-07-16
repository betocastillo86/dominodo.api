using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Contracts;

namespace Dominodo.Users.Application.Permissions.GetPermissions;

internal sealed record GetPermissionsQuery : IQuery<IReadOnlyList<PermissionDto>>;
