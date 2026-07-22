using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Contracts;
using FluentValidation;

namespace Dominodo.Operations.Application.Deliveries.MarkDeliveryDelivered;

// Received/Notified → Delivered (deliveries.edit). ReceivedByName is the free-text default (domain-model
// §3.2); DeliveredToUserId is the optional system-user path, validated against Users when supplied.
internal sealed record MarkDeliveryDeliveredCommand(
    Guid DeliveryId,
    string? ReceivedByName,
    Guid? DeliveredToUserId) : ICommand;

internal sealed class MarkDeliveryDeliveredCommandValidator : AbstractValidator<MarkDeliveryDeliveredCommand>
{
    public MarkDeliveryDeliveredCommandValidator()
    {
        RuleFor(x => x.ReceivedByName).MaximumLength(200);
    }
}

internal sealed class MarkDeliveryDeliveredCommandHandler(
    IDeliveryRepository deliveries,
    IUsersModuleApi usersModule,
    IClock clock)
    : ICommandHandler<MarkDeliveryDeliveredCommand>
{
    public async Task<Result> Handle(MarkDeliveryDeliveredCommand command, CancellationToken ct)
    {
        var delivery = await deliveries.GetByIdAsync(command.DeliveryId, ct);
        if (delivery is null)
        {
            return Error.NotFound("Delivery.NotFound", "Delivery not found.");
        }

        if (command.DeliveredToUserId is { } userId)
        {
            var user = await usersModule.GetUserByIdAsync(userId, ct);
            if (user is null)
            {
                return Error.NotFound("User.NotFound", "The referenced user does not exist.");
            }
        }

        return delivery.MarkDelivered(clock.UtcNow, command.ReceivedByName, command.DeliveredToUserId);
    }
}
