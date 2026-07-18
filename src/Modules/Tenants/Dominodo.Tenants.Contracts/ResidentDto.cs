namespace Dominodo.Tenants.Contracts;

public sealed record ResidentDto(
    Guid Id,
    Guid ApartmentId,
    Guid UserId,
    string RelationType,
    bool LivesHere,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsActive);
