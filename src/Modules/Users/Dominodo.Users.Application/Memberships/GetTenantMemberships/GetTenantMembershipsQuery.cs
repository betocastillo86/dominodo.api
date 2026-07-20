using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Contracts;

namespace Dominodo.Users.Application.Memberships.GetTenantMemberships;

internal sealed record GetTenantMembershipsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<MembershipDto>>;
