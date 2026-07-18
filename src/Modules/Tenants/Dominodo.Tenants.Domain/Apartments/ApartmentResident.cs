namespace Dominodo.Tenants.Domain.Apartments;

// The resident↔apartment link (domain-model §2.4) — a child entity under the Apartment aggregate, only
// mutated through Apartment. UserId is a raw Guid from the Users module (NO cross-module FK). Multi-owner
// is supported: several active Owner rows for the same apartment are allowed.
public sealed class ApartmentResident
{
    private ApartmentResident() { } // EF Core

    internal ApartmentResident(
        Guid id,
        Guid apartmentId,
        Guid tenantId,
        Guid userId,
        ResidentRelationType relationType,
        bool livesHere,
        DateOnly? startDate)
    {
        Id = id;
        ApartmentId = apartmentId;
        TenantId = tenantId;
        UserId = userId;
        RelationType = relationType;
        LivesHere = livesHere;
        StartDate = startDate;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid ApartmentId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public ResidentRelationType RelationType { get; private set; }
    public bool LivesHere { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; }

    internal void End(DateOnly endDate)
    {
        EndDate = endDate;
        IsActive = false;
    }
}
