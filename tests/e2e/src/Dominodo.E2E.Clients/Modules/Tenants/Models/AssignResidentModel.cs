namespace Dominodo.E2E.Clients.Modules.Tenants.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/apartments/{apartmentId}/residents</c>. Mirrors the
/// API's <c>AssignResidentRequest</c> by value. <c>RelationType</c> is a <c>ResidentRelationType</c> name
/// ("Owner" or "Renter").
/// </summary>
public sealed record AssignResidentModel
{
    public Guid UserId { get; init; }
    public string RelationType { get; init; } = default!;
    public bool LivesHere { get; init; }
    public DateOnly? StartDate { get; init; }
}
