using Dominodo.Admin.Contracts;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Admin.Application.Notifications.GetMyNotifications;

// Self-service (domain-model §4.2 / doc 12 ownership): returns ONLY the caller's own in-app
// notifications, keyed to ICurrentUser.UserId — not gated by a notifications.* permission.
internal sealed record GetMyNotificationsQuery(bool UnreadOnly = false, int Page = 1, int PageSize = 20) : IQuery<PagedResult<InAppMessageDto>>;

internal sealed class GetMyNotificationsQueryHandler(
    IInAppMessageRepository notifications,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyNotificationsQuery, PagedResult<InAppMessageDto>>
{
    public async Task<Result<PagedResult<InAppMessageDto>>> Handle(GetMyNotificationsQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await notifications.GetForRecipientAsync(currentUser.UserId, query.UnreadOnly, page, ct);
        var dtos = items.Select(NotificationMappers.ToDto).ToList();
        return new PagedResult<InAppMessageDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }
}
