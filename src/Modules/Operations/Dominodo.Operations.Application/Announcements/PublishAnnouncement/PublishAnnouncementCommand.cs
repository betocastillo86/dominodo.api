using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Operations.Application.Announcements.PublishAnnouncement;

// Draft → Published (announcements.edit). Raises AnnouncementPublishedDomainEvent (→ Admin materializes
// the notification to the audience).
internal sealed record PublishAnnouncementCommand(Guid AnnouncementId) : ICommand;

internal sealed class PublishAnnouncementCommandHandler(
    IAnnouncementRepository announcements,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<PublishAnnouncementCommand>
{
    public async Task<Result> Handle(PublishAnnouncementCommand command, CancellationToken ct)
    {
        var announcement = await announcements.GetByIdAsync(command.AnnouncementId, ct);
        if (announcement is null)
        {
            return Error.NotFound("Announcement.NotFound", "Announcement not found.");
        }

        return announcement.Publish(currentUser.UserId, clock.UtcNow);
    }
}
