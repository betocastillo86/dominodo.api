using Dominodo.Shared.Kernel;
using Dominodo.Users.Domain.Memberships.Events;

namespace Dominodo.Users.Domain.Memberships;

// Grants a user a Tenant-scope role within one conjunto (domain-model §1.6). The tenant-owned
// counterpart of PlatformRoleAssignment: it is what turns on the tenant branch of permission
// resolution. Holds internal FKs to User and Role (same module); TenantId is a raw Guid — Tenant
// lives in another module, so no cross-boundary FK (rule #2). Only an Active membership grants
// permissions. Every mutating read is scoped by TenantId (ForCurrentTenant, doc 09).
public sealed class Membership : AggregateRoot, ITenantOwned
{
    private Membership() { } // EF Core

    private Membership(Guid id, Guid userId, Guid tenantId, int roleId, DateTimeOffset invitedAtUtc) : base(id)
    {
        UserId = userId;
        TenantId = tenantId;
        RoleId = roleId;
        Status = MembershipStatus.Invited;
        InvitedAtUtc = invitedAtUtc;
    }

    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public int RoleId { get; private set; }
    public MembershipStatus Status { get; private set; }
    public DateTimeOffset? InvitedAtUtc { get; private set; }
    public DateTimeOffset? JoinedAtUtc { get; private set; }

    public static Result<Membership> Invite(Guid userId, Guid tenantId, int roleId, DateTimeOffset nowUtc)
    {
        if (userId == Guid.Empty)
        {
            return Error.Validation("Membership.UserRequired", "A user is required.");
        }

        if (tenantId == Guid.Empty)
        {
            return Error.Validation("Membership.TenantRequired", "A tenant is required.");
        }

        var membership = new Membership(Guid.NewGuid(), userId, tenantId, roleId, nowUtc);
        membership.Raise(new MembershipCreatedDomainEvent(membership.Id, membership.UserId, membership.TenantId, membership.RoleId));
        return membership;
    }

    // Self-service: the invited user accepts and becomes Active.
    public Result Accept(DateTimeOffset nowUtc)
    {
        if (Status != MembershipStatus.Invited)
        {
            return Error.Conflict("Membership.NotInvited", "Only an invited membership can be accepted.");
        }

        Status = MembershipStatus.Active;
        JoinedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result ChangeRole(int roleId)
    {
        if (roleId == RoleId)
        {
            return Result.Success();
        }

        RoleId = roleId;
        Raise(new MembershipRoleChangedDomainEvent(Id, UserId, TenantId, RoleId));
        return Result.Success();
    }

    public Result Suspend()
    {
        if (Status != MembershipStatus.Active)
        {
            return Error.Conflict("Membership.NotActive", "Only an active membership can be suspended.");
        }

        Status = MembershipStatus.Suspended;
        Raise(new MembershipSuspendedDomainEvent(Id, UserId, TenantId));
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status != MembershipStatus.Suspended)
        {
            return Error.Conflict("Membership.NotSuspended", "Only a suspended membership can be reactivated.");
        }

        Status = MembershipStatus.Active;
        Raise(new MembershipRoleChangedDomainEvent(Id, UserId, TenantId, RoleId));
        return Result.Success();
    }
}
