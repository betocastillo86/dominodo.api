using Dominodo.Users.Domain.Roles;
using FluentValidation;

namespace Dominodo.Users.Application.Roles.CreateRole;

internal sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);

        RuleFor(x => x.Scope)
            .Must(scope => Enum.TryParse<RoleScope>(scope, ignoreCase: false, out _))
            .WithMessage("Scope must be either 'Platform' or 'Tenant'.");

        RuleFor(x => x.PermissionIds)
            .NotNull();
    }
}
