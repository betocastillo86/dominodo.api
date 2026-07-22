using Dominodo.Admin.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Admin.Application.Devices.DeactivateDevice;

// Self-service: deactivates the CURRENT user's device. A device owned by another user is a leak-safe 404.
internal sealed record DeactivateDeviceCommand(Guid Id) : ICommand;

internal sealed class DeactivateDeviceCommandHandler(
    IDeviceRegistrationRepository devices,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<DeactivateDeviceCommand>
{
    public async Task<Result> Handle(DeactivateDeviceCommand command, CancellationToken ct)
    {
        var device = await devices.GetByIdAsync(command.Id, ct);
        if (device is null || device.UserId != currentUser.UserId)
        {
            return Error.NotFound("DeviceRegistration.NotFound", "No device found for this id.");
        }

        device.Deactivate(clock);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return Result.Success();
    }
}
