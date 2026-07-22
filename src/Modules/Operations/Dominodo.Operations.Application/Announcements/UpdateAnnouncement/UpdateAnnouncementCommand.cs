using Dominodo.Operations.Domain.Announcements;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Operations.Application.Announcements.UpdateAnnouncement;

// Edits an announcement (announcements.edit). Publish/archive have their own commands.
internal sealed record UpdateAnnouncementCommand(
    Guid AnnouncementId,
    string Title,
    string Body,
    byte Priority,
    AudienceType AudienceType,
    string? AudienceFilter,
    string? Category,
    DateTimeOffset? ExpiresAtUtc) : ICommand;

internal sealed class UpdateAnnouncementCommandValidator : AbstractValidator<UpdateAnnouncementCommand>
{
    public UpdateAnnouncementCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.AudienceType).IsInEnum();
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.AudienceFilter)
            .NotEmpty()
            .When(x => x.AudienceType is AudienceType.ByTower or AudienceType.ByApartments)
            .WithMessage("An audience filter is required for ByTower / ByApartments announcements.");
    }
}

internal sealed class UpdateAnnouncementCommandHandler(IAnnouncementRepository announcements)
    : ICommandHandler<UpdateAnnouncementCommand>
{
    public async Task<Result> Handle(UpdateAnnouncementCommand command, CancellationToken ct)
    {
        var announcement = await announcements.GetByIdAsync(command.AnnouncementId, ct);
        if (announcement is null)
        {
            return Error.NotFound("Announcement.NotFound", "Announcement not found.");
        }

        return announcement.Update(
            command.Title,
            command.Body,
            command.Priority,
            command.AudienceType,
            command.AudienceFilter,
            command.Category,
            command.ExpiresAtUtc);
    }
}
