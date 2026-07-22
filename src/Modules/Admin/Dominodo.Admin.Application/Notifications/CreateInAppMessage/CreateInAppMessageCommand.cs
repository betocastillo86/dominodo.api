using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Admin.Application.Notifications.CreateInAppMessage;

// Admin side (notifications.create): materializes an in-app notification for a recipient. TenantId comes
// from ITenantContext (recipients are tenant-scoped, so an X-Tenant is required); TriggeredByUserId is
// the acting admin.
internal sealed record CreateInAppMessageCommand(
    Guid RecipientUserId,
    NotificationType Type,
    string Title,
    string Body,
    string? TargetUrl) : ICommand<Guid>;

internal sealed class CreateInAppMessageCommandValidator : AbstractValidator<CreateInAppMessageCommand>
{
    public CreateInAppMessageCommandValidator()
    {
        RuleFor(x => x.RecipientUserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
    }
}

internal sealed class CreateInAppMessageCommandHandler(
    IInAppMessageRepository notifications,
    ITenantContext tenant,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<CreateInAppMessageCommand, Guid>
{
    public Task<Result<Guid>> Handle(CreateInAppMessageCommand command, CancellationToken ct)
    {
        if (!tenant.HasTenant)
        {
            return Task.FromResult<Result<Guid>>(
                Error.Validation("InAppMessage.TenantRequired", "An X-Tenant header is required to create a notification."));
        }

        var result = InAppMessage.Create(
            tenant.TenantId,
            command.RecipientUserId,
            command.Type,
            command.Title,
            command.Body,
            command.TargetUrl,
            currentUser.UserId,
            clock);

        if (result.IsFailure)
        {
            return Task.FromResult<Result<Guid>>(result.Error);
        }

        notifications.Add(result.Value);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Task.FromResult<Result<Guid>>(result.Value.Id);
    }
}
