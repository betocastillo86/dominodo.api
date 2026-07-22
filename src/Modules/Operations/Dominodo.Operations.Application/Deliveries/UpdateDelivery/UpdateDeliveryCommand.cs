using Dominodo.Operations.Domain.Deliveries;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using FluentValidation;

namespace Dominodo.Operations.Application.Deliveries.UpdateDelivery;

// Edits delivery details (deliveries.edit). Status changes go through the Mark* commands.
internal sealed record UpdateDeliveryCommand(
    Guid DeliveryId,
    DeliveryType Type,
    string? Carrier,
    string? Comment,
    string? PhotoUrl,
    string? Metadata) : ICommand;

internal sealed class UpdateDeliveryCommandValidator : AbstractValidator<UpdateDeliveryCommand>
{
    public UpdateDeliveryCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Carrier).MaximumLength(100);
        RuleFor(x => x.Comment).MaximumLength(1000);
        RuleFor(x => x.PhotoUrl).MaximumLength(2000);
    }
}

internal sealed class UpdateDeliveryCommandHandler(IDeliveryRepository deliveries)
    : ICommandHandler<UpdateDeliveryCommand>
{
    public async Task<Result> Handle(UpdateDeliveryCommand command, CancellationToken ct)
    {
        var delivery = await deliveries.GetByIdAsync(command.DeliveryId, ct);
        if (delivery is null)
        {
            return Error.NotFound("Delivery.NotFound", "Delivery not found.");
        }

        var result = delivery.UpdateDetails(command.Type, command.Carrier, command.Comment, command.PhotoUrl);
        if (result.IsFailure)
        {
            return result.Error;
        }

        delivery.SetMetadata(command.Metadata);
        return Result.Success();
    }
}
