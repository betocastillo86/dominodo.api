using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Apartments.Residents.AssignResident;

internal sealed record AssignResidentCommand(
    Guid ApartmentId,
    Guid UserId,
    string RelationType,
    bool LivesHere,
    DateOnly? StartDate) : ICommand<Guid>;
