using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Admin.Application.Notifications.UpdateNotificationTemplate;

internal sealed record UpdateNotificationTemplateCommand(
    Guid Id,
    string Channels,
    string? EmailSubject,
    string? EmailBodyHtml,
    string? InAppText,
    string? PushText,
    bool IsActive,
    string? Localization) : ICommand;

internal sealed class UpdateNotificationTemplateCommandValidator : AbstractValidator<UpdateNotificationTemplateCommand>
{
    public UpdateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Channels)
            .Must(c => Enum.TryParse<NotificationChannel>(c, ignoreCase: false, out _))
            .WithMessage("Channels must be a combination of 'Email', 'Push', 'InApp' (e.g. 'Email, InApp').");
    }
}

// The edited row must belong to the current scope: with an X-Tenant its TenantId is that tenant (an
// override), without one it is null (a global default). Because Administrador holds notifications.edit
// only through a tenant membership, the permission resolves only with X-Tenant — so it can never target
// the global row (scope mismatch → 404). Editing a global template needs a Platform-role token with no
// X-Tenant. No role-name check (doc 12 / §4.1).
internal sealed class UpdateNotificationTemplateCommandHandler(
    INotificationTemplateRepository templates,
    ITenantContext tenant)
    : ICommandHandler<UpdateNotificationTemplateCommand>
{
    public async Task<Result> Handle(UpdateNotificationTemplateCommand command, CancellationToken ct)
    {
        var template = await templates.GetByIdAsync(command.Id, ct);

        Guid? scopeTenantId = tenant.HasTenant ? tenant.TenantId : null;
        if (template is null || template.TenantId != scopeTenantId)
        {
            return Error.NotFound("NotificationTemplate.NotFound", "No notification template found for this id in the current scope.");
        }

        var channels = Enum.Parse<NotificationChannel>(command.Channels);

        template.UpdateContent(
            channels,
            command.EmailSubject,
            command.EmailBodyHtml,
            command.InAppText,
            command.PushText,
            command.IsActive,
            command.Localization);

        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Result.Success();
    }
}
