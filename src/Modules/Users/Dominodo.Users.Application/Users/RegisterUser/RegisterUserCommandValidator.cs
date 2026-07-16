using FluentValidation;

namespace Dominodo.Users.Application.Users.RegisterUser;

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
