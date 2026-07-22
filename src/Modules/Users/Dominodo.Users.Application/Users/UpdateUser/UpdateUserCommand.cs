using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.ValueObjects;
using FluentValidation;

namespace Dominodo.Users.Application.Users.UpdateUser;

internal sealed record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Email,
    string PreferredLanguage) : ICommand;

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PreferredLanguage).MaximumLength(10);
    }
}

internal sealed class UpdateUserCommandHandler(IUserRepository users)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", "User not found.");
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

            if (!string.Equals(user.Email, email.Value, StringComparison.OrdinalIgnoreCase)
                && await users.ExistsByEmailAsync(email.Value, ct))
            {
                return Error.Conflict("User.EmailAlreadyRegistered", "A user with this email already exists.");
            }
        }

        var updateResult = user.UpdateProfile(command.FirstName, command.LastName, email, command.PreferredLanguage);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Result.Success();
    }
}
