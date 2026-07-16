using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Application.Abstractions;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Users;
using Dominodo.Users.Domain.ValueObjects;

namespace Dominodo.Users.Application.Users.RegisterUser;

internal sealed class RegisterUserCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken ct)
    {
        var phoneResult = PhoneNumber.Create(command.Phone);
        if (phoneResult.IsFailure)
        {
            return phoneResult.Error;
        }

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var emailResult = Email.Create(command.Email);
            if (emailResult.IsFailure)
            {
                return emailResult.Error;
            }

            email = emailResult.Value;
        }

        if (await users.ExistsByPhoneAsync(phoneResult.Value.Value, ct))
        {
            return Error.Conflict("User.PhoneAlreadyRegistered", "A user with this phone already exists.");
        }

        if (email is not null && await users.ExistsByEmailAsync(email.Value, ct))
        {
            return Error.Conflict("User.EmailAlreadyRegistered", "A user with this email already exists.");
        }

        var passwordHash = passwordHasher.Hash(command.Password);

        var userResult = User.Register(phoneResult.Value, email, command.FirstName, command.LastName, passwordHash);
        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        users.Add(userResult.Value);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return userResult.Value.Id;
    }
}
