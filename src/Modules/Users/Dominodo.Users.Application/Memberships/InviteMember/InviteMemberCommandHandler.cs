using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Memberships;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Roles;

namespace Dominodo.Users.Application.Memberships.InviteMember;

// Invites an already-registered user (by phone) into the current conjunto with a Tenant-scope role.
// TenantId comes from ITenantContext — the X-Tenant header already resolved to a live tenant via the
// directory (Active/Onboarding only), so tenant existence is NOT re-validated and no cross-module read
// is introduced. Only Tenant-scope roles are grantable here (Platform roles are minted via platform
// assignments, not memberships).
internal sealed class InviteMemberCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IMembershipRepository memberships,
    ITenantContext tenant,
    IClock clock)
    : ICommandHandler<InviteMemberCommand, Guid>
{
    public async Task<Result<Guid>> Handle(InviteMemberCommand command, CancellationToken ct)
    {
        var tenantId = tenant.TenantId;

        var user = await users.GetByPhoneAsync(command.Phone.Trim(), ct);
        if (user is null)
        {
            return Error.NotFound("Membership.UserNotFound", "No registered user with this phone.");
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

        var existing = await memberships.GetByUserAndTenantAsync(user.Id, tenantId, ct);
        if (existing is not null)
        {
            return Error.Conflict("Membership.AlreadyExists", "This user already has a membership in this conjunto.");
        }

        var membershipResult = Membership.Invite(user.Id, tenantId, command.RoleId, clock.UtcNow);
        if (membershipResult.IsFailure)
        {
            return membershipResult.Error;
        }

        var membership = membershipResult.Value;
        memberships.Add(membership);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return membership.Id;
    }
}
