using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Apartments.Residents.EndResidency;

internal sealed record EndResidencyCommand(Guid ApartmentId, Guid ResidentId, DateOnly EndDate) : ICommand;
