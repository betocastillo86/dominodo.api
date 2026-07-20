using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Roles;
using FluentValidation;

namespace Dominodo.Users.Application.Memberships.ChangeMemberRole;

internal sealed record ChangeMemberRoleCommand(Guid MembershipId, int RoleId) : ICommand;

internal sealed class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}

// Tenant-scoped: the membership is loaded ForCurrentTenant, so an admin can only re-role members of their
// own conjunto. The new role must be Tenant-scoped.
internal sealed class ChangeMemberRoleCommandHandler(
    IMembershipRepository memberships,
    IRoleRepository roles)
    : ICommandHandler<ChangeMemberRoleCommand>
{
    public async Task<Result> Handle(ChangeMemberRoleCommand command, CancellationToken ct)
    {
        var membership = await memberships.GetByIdForCurrentTenantAsync(command.MembershipId, ct);
        if (membership is null)
        {
            return Error.NotFound("Membership.NotFound", "Membership not found in this conjunto.");
        }

        var role = await roles.GetByIdAsync(command.RoleId, ct);
        if (role is null)
        {
            return Error.Validation("Membership.RoleNotFound", "The role does not exist.");
        }

        if (role.Scope != RoleScope.Tenant)
        {
            return Error.Validation("Membership.RoleNotTenantScoped", "Only a tenant-scoped role can be assigned in a conjunto.");
        }

        return membership.ChangeRole(command.RoleId);
    }
}
