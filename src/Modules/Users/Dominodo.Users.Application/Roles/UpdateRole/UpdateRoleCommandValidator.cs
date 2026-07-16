using FluentValidation;

namespace Dominodo.Users.Application.Roles.UpdateRole;

internal sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.PermissionIds).NotNull();
    }
}
