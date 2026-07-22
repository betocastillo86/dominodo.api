using Dominodo.Admin.Domain.Devices;
using Dominodo.Admin.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Admin.Persistence.Repositories;

internal sealed class DeviceRegistrationRepository(AdminDbContext db) : IDeviceRegistrationRepository
{
    public void Add(DeviceRegistration device) => db.DeviceRegistrations.Add(device);

    public Task<DeviceRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.DeviceRegistrations.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<DeviceRegistration?> GetByUserAndTokenAsync(Guid userId, string token, CancellationToken cancellationToken = default) =>
        db.DeviceRegistrations.FirstOrDefaultAsync(d => d.UserId == userId && d.Token == token, cancellationToken);
}
