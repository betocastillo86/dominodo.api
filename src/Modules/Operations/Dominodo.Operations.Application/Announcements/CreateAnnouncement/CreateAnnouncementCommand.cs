using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Announcements;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Operations.Application.Announcements.CreateAnnouncement;

// Creates an announcement draft (announcements.create). AudienceFilter is raw JSON: a string[] of towers
// for ByTower, a Guid[] of apartment ids for ByApartments; ignored for AllTenant.
internal sealed record CreateAnnouncementCommand(
    string Title,
    string Body,
    byte Priority,
    AudienceType AudienceType,
    string? AudienceFilter,
    string? Category,
    DateTimeOffset? ExpiresAtUtc) : ICommand<AnnouncementDto>;

internal sealed class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
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

internal sealed class CreateAnnouncementCommandHandler(
    IAnnouncementRepository announcements,
    ITenantContext tenant)
    : ICommandHandler<CreateAnnouncementCommand, AnnouncementDto>
{
    public Task<Result<AnnouncementDto>> Handle(CreateAnnouncementCommand command, CancellationToken ct)
    {
        var result = Announcement.CreateDraft(
            tenant.TenantId,
            command.Title,
            command.Body,
            command.Priority,
            command.AudienceType,
            command.AudienceFilter,
            command.Category,
            command.ExpiresAtUtc);
        if (result.IsFailure)
        {
            return Task.FromResult(Result.Failure<AnnouncementDto>(result.Error));
        }

        announcements.Add(result.Value);
        return Task.FromResult(Result.Success(AnnouncementMappers.ToDto(result.Value)));
    }
}
