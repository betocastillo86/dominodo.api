using Dominodo.Admin.Domain.Devices;

namespace Dominodo.Admin.Domain.Ports;

public interface IDeviceRegistrationRepository
{
    void Add(DeviceRegistration device);

    Task<DeviceRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // A device is identified by (UserId, Token) — re-registering the same token upserts.
    Task<DeviceRegistration?> GetByUserAndTokenAsync(Guid userId, string token, CancellationToken cancellationToken = default);
}
