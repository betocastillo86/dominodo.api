using Dominodo.Admin.Domain.Notifications;
using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Admin.Application.Notifications.CreateUserNotification;

// Admin side (notifications.create): materializes an in-app notification for a recipient. TenantId comes
// from ITenantContext (recipients are tenant-scoped, so an X-Tenant is required); TriggeredByUserId is
// the acting admin.
internal sealed record CreateUserNotificationCommand(
    Guid RecipientUserId,
    string Type,
    string Title,
    string Body,
    string? TargetUrl) : ICommand<Guid>;

internal sealed class CreateUserNotificationCommandValidator : AbstractValidator<CreateUserNotificationCommand>
{
    public CreateUserNotificationCommandValidator()
    {
        RuleFor(x => x.RecipientUserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Type)
            .Must(t => Enum.TryParse<NotificationType>(t, ignoreCase: false, out _))
            .WithMessage("Type must be a valid notification type.");
    }
}

internal sealed class CreateUserNotificationCommandHandler(
    IUserNotificationRepository notifications,
    ITenantContext tenant,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<CreateUserNotificationCommand, Guid>
{
    public Task<Result<Guid>> Handle(CreateUserNotificationCommand command, CancellationToken ct)
    {
        if (!tenant.HasTenant)
        {
            return Task.FromResult<Result<Guid>>(
                Error.Validation("UserNotification.TenantRequired", "An X-Tenant header is required to create a notification."));
        }

        var type = Enum.Parse<NotificationType>(command.Type);

        var result = UserNotification.Create(
            tenant.TenantId,
            command.RecipientUserId,
            type,
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
