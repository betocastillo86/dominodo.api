using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Operations.Application.Announcements.ArchiveAnnouncement;

// Draft/Published → Archived (announcements.edit).
internal sealed record ArchiveAnnouncementCommand(Guid AnnouncementId) : ICommand;

internal sealed class ArchiveAnnouncementCommandHandler(IAnnouncementRepository announcements)
    : ICommandHandler<ArchiveAnnouncementCommand>
{
    public async Task<Result> Handle(ArchiveAnnouncementCommand command, CancellationToken ct)
    {
        var announcement = await announcements.GetByIdAsync(command.AnnouncementId, ct);
        if (announcement is null)
        {
            return Error.NotFound("Announcement.NotFound", "Announcement not found.");
        }

        return announcement.Archive();
    }
}
