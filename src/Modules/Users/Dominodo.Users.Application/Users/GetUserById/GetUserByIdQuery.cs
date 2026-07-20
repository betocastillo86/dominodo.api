using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Contracts;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Users.GetUserById;

internal sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserDto>;

internal sealed class GetUserByIdQueryHandler(IUserRepository users)
    : IQueryHandler<GetUserByIdQuery, UserDto>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(query.UserId, ct);

        return user is null
            ? Error.NotFound("User.NotFound", "User not found.")
            : new UserDto(
                user.Id,
                user.Phone,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Status.ToString(),
                user.PhoneVerifiedAtUtc is not null);
    }
}
