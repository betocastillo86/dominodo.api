using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Operations.Application.Announcements.GetAnnouncementById;

// Admin read (announcements.view) — any announcement incl. drafts.
internal sealed record GetAnnouncementByIdQuery(Guid AnnouncementId) : IQuery<AnnouncementDetailDto>;

internal sealed class GetAnnouncementByIdQueryHandler(IAnnouncementRepository announcements)
    : IQueryHandler<GetAnnouncementByIdQuery, AnnouncementDetailDto>
{
    public async Task<Result<AnnouncementDetailDto>> Handle(GetAnnouncementByIdQuery query, CancellationToken ct)
    {
        var announcement = await announcements.GetByIdAsync(query.AnnouncementId, ct);
        if (announcement is null)
        {
            return Error.NotFound("Announcement.NotFound", "Announcement not found.");
        }

        return AnnouncementMappers.ToDetailDto(announcement);
    }
}
