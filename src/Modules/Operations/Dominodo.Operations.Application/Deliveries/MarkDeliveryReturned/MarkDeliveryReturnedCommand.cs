using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Operations.Application.Deliveries.MarkDeliveryReturned;

// Received/Notified → Returned (deliveries.edit).
internal sealed record MarkDeliveryReturnedCommand(Guid DeliveryId) : ICommand;

internal sealed class MarkDeliveryReturnedCommandHandler(IDeliveryRepository deliveries)
    : ICommandHandler<MarkDeliveryReturnedCommand>
{
    public async Task<Result> Handle(MarkDeliveryReturnedCommand command, CancellationToken ct)
    {
        var delivery = await deliveries.GetByIdAsync(command.DeliveryId, ct);
        if (delivery is null)
        {
            return Error.NotFound("Delivery.NotFound", "Delivery not found.");
        }

        return delivery.MarkReturned();
    }
}
