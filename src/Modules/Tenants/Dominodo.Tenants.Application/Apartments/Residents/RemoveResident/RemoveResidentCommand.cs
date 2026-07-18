using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Apartments.Residents.RemoveResident;

internal sealed record RemoveResidentCommand(Guid ApartmentId, Guid ResidentId) : ICommand;
