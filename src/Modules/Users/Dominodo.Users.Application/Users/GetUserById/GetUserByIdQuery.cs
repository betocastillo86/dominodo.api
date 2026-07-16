using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Contracts;

namespace Dominodo.Users.Application.Users.GetUserById;

internal sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserDto>;
