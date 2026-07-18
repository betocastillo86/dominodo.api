using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Apartments.UpdateApartment;

internal sealed record UpdateApartmentCommand(
    Guid ApartmentId,
    string Number,
    string Type,
    string? Tower,
    string? Attributes) : ICommand;
