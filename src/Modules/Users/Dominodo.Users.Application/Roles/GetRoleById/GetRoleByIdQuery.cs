using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Roles.GetRoleById;

internal sealed record GetRoleByIdQuery(int RoleId) : IQuery<RoleDetailDto>;
