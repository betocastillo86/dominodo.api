namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated response item for <c>GET /api/v1/apartments/{apartmentId}/residents</c>. Mirrors the
/// API's <c>ResidentDto</c> by value. <c>RelationType</c> is a <c>ResidentRelationType</c> name ("Owner" or
/// "Renter"); <c>IsActive</c> flips off once the residency is ended.
/// </summary>
public sealed record ResidentModel
{
    public Guid Id { get; init; }
    public Guid ApartmentId { get; init; }
    public Guid UserId { get; init; }
    public string RelationType { get; init; } = default!;
    public bool LivesHere { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
}
