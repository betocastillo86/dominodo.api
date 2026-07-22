using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Announcements;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Shared.Kernel.Pagination;

namespace Dominodo.Operations.Application.Announcements.GetAnnouncements;

// Admin listing (announcements.view) — includes drafts.
internal sealed record GetAnnouncementsQuery(
    int Page = 1,
    int PageSize = 20,
    AnnouncementStatus? Status = null,
    string? Category = null) : IQuery<PagedResult<AnnouncementDto>>;

internal sealed class GetAnnouncementsQueryHandler(IAnnouncementRepository announcements)
    : IQueryHandler<GetAnnouncementsQuery, PagedResult<AnnouncementDto>>
{
    public async Task<Result<PagedResult<AnnouncementDto>>> Handle(GetAnnouncementsQuery query, CancellationToken ct)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var (items, total) = await announcements.ListAsync(page, query.Status, query.Category, ct);

        var dtos = items.Select(AnnouncementMappers.ToDto).ToList();
        return new PagedResult<AnnouncementDto>(dtos, page.Skip / page.Take + 1, page.Take, total);
    }
}
