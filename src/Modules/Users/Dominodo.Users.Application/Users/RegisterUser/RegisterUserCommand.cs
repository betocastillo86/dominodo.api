using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Application.Abstractions;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Users;
using Dominodo.Users.Domain.ValueObjects;
using FluentValidation;

namespace Dominodo.Users.Application.Users.RegisterUser;

internal sealed record RegisterUserCommand(
    string Phone,
    string? Email,
    string FirstName,
    string LastName,
    string Password) : ICommand<Guid>;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Phone must be in E.164 format (e.g. +573001234567).");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

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
