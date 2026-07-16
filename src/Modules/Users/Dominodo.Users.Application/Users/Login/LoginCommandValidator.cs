using FluentValidation;

namespace Dominodo.Users.Application.Users.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
