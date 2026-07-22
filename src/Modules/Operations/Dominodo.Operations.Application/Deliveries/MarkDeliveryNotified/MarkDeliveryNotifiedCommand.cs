using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Operations.Application.Deliveries.MarkDeliveryNotified;

// Received → Notified (deliveries.edit).
internal sealed record MarkDeliveryNotifiedCommand(Guid DeliveryId) : ICommand;

internal sealed class MarkDeliveryNotifiedCommandHandler(IDeliveryRepository deliveries)
    : ICommandHandler<MarkDeliveryNotifiedCommand>
{
    public async Task<Result> Handle(MarkDeliveryNotifiedCommand command, CancellationToken ct)
    {
        var delivery = await deliveries.GetByIdAsync(command.DeliveryId, ct);
        if (delivery is null)
        {
            return Error.NotFound("Delivery.NotFound", "Delivery not found.");
        }

        return delivery.MarkNotified();
    }
}
