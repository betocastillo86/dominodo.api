using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Contracts;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Users;

namespace Dominodo.Users.Application.Users.ListUsers;

internal sealed record ListUsersQuery(
    Guid? TenantId = null,
    string? Name = null,
    string? Email = null,
    string? Phone = null,
    UserStatus? Status = null,
    string? DocumentNumber = null,
    bool? PhoneVerified = null,
    bool? EmailVerified = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<UserListItemDto>>;

internal sealed class ListUsersQueryHandler(IUserRepository users)
    : IQueryHandler<ListUsersQuery, PagedResult<UserListItemDto>>
{
    public async Task<Result<PagedResult<UserListItemDto>>> Handle(ListUsersQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await users.ListAsync(
            page,
            query.TenantId,
            query.Name,
            query.Email,
            query.Phone,
            query.Status,
            query.DocumentNumber,
            query.PhoneVerified,
            query.EmailVerified,
            ct);

        var dtos = items.Select(ToDto).ToList();
        return new PagedResult<UserListItemDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }

    private static UserListItemDto ToDto(User user) => new(
        user.Id,
        user.Phone,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Status.ToString(),
        user.DocumentType?.ToString(),
        user.DocumentNumber,
        user.PhoneVerifiedAtUtc is not null,
        user.EmailVerifiedAtUtc is not null);
}
