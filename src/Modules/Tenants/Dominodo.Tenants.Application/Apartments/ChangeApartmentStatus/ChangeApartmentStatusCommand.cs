using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Apartments.ChangeApartmentStatus;

internal sealed record ChangeApartmentStatusCommand(Guid ApartmentId, string Status) : ICommand;
