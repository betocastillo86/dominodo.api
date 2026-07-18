using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Apartments.CreateApartment;

internal sealed record CreateApartmentCommand(
    string Number,
    string Type,
    string? Tower,
    string? Attributes) : ICommand<Guid>;
