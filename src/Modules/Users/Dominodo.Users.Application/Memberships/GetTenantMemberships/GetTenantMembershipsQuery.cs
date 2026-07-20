using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Contracts;
using Dominodo.Users.Domain.Memberships;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Memberships.GetTenantMemberships;

internal sealed record GetTenantMembershipsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<MembershipDto>>;

// Lists the current tenant's memberships (scoped via ListForCurrentTenantAsync), paged in memory and
// joined to role names for display.
internal sealed class GetTenantMembershipsQueryHandler(
    IMembershipRepository memberships,
    IRoleRepository roles)
    : IQueryHandler<GetTenantMembershipsQuery, PagedResult<MembershipDto>>
{
    public async Task<Result<PagedResult<MembershipDto>>> Handle(GetTenantMembershipsQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var all = await memberships.ListForCurrentTenantAsync(ct);
        var total = all.Count;

        var pageItems = all.Skip(page.Skip).Take(page.Take).ToList();

        var roleNames = new Dictionary<int, string>();
        foreach (var roleId in pageItems.Select(m => m.RoleId).Distinct())
        {
            var role = await roles.GetByIdAsync(roleId, ct);
            if (role is not null)
            {
                roleNames[roleId] = role.Name;
            }
        }

        var dtos = pageItems.Select(m => ToDto(m, roleNames.GetValueOrDefault(m.RoleId, string.Empty))).ToList();
        return new PagedResult<MembershipDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }

    private static MembershipDto ToDto(Membership m, string roleName) => new(
        m.UserId,
        m.TenantId,
        m.RoleId,
        roleName,
        m.Status.ToString(),
        m.InvitedAtUtc,
        m.JoinedAtUtc);
}
