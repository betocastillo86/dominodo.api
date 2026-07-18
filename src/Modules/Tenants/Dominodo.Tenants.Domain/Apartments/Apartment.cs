using Dominodo.Shared.Kernel;
using Dominodo.Tenants.Domain.Apartments.Events;

namespace Dominodo.Tenants.Domain.Apartments;

// An apartment within a conjunto — the first ITenantOwned aggregate (domain-model §2.3). Every read is
// scoped by TenantId (ForCurrentTenant, doc 09); TenantId is set once at creation and never changes.
public sealed class Apartment : AggregateRoot, ITenantOwned
{
    private readonly List<ApartmentResident> _residents = new();

    private Apartment() { } // EF Core

    private Apartment(
        Guid id,
        Guid tenantId,
        string number,
        ApartmentType type,
        string? tower) : base(id)
    {
        TenantId = tenantId;
        Number = number;
        Type = type;
        Tower = tower;
        Status = ApartmentStatus.Vacant;
    }

    public Guid TenantId { get; private set; }
    public string? Tower { get; private set; }
    public string Number { get; private set; } = null!;
    public ApartmentType Type { get; private set; }
    public ApartmentStatus Status { get; private set; }
    public string? Attributes { get; private set; }

    public IReadOnlyCollection<ApartmentResident> Residents => _residents.AsReadOnly();

    public static Result<Apartment> Create(
        Guid tenantId,
        string number,
        ApartmentType type,
        string? tower = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Error.Validation("Apartment.TenantRequired", "A tenant is required.");
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            return Error.Validation("Apartment.NumberRequired", "Apartment number is required.");
        }

        var apartment = new Apartment(
            Guid.NewGuid(),
            tenantId,
            number.Trim(),
            type,
            string.IsNullOrWhiteSpace(tower) ? null : tower.Trim());

        apartment.Raise(new ApartmentCreatedDomainEvent(apartment.Id, apartment.TenantId));
        return apartment;
    }

    public Result Rename(string number, string? tower)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return Error.Validation("Apartment.NumberRequired", "Apartment number is required.");
        }

        Number = number.Trim();
        Tower = string.IsNullOrWhiteSpace(tower) ? null : tower.Trim();
        return Result.Success();
    }

    public Result ChangeType(ApartmentType type)
    {
        Type = type;
        return Result.Success();
    }

    public void SetAttributes(string? attributes) => Attributes = attributes;

    public Result MarkOccupied()
    {
        if (Status == ApartmentStatus.Occupied)
        {
            return Error.Conflict("Apartment.AlreadyOccupied", "The apartment is already occupied.");
        }

        Status = ApartmentStatus.Occupied;
        return Result.Success();
    }

    public Result MarkVacant()
    {
        if (Status == ApartmentStatus.Vacant)
        {
            return Error.Conflict("Apartment.AlreadyVacant", "The apartment is already vacant.");
        }

        Status = ApartmentStatus.Vacant;
        return Result.Success();
    }

    // Attaches a resident. Multi-owner is allowed (several active Owner rows), but the same user cannot
    // hold two ACTIVE links to the same apartment. Caller validates the user exists in Users (facade).
    public Result<ApartmentResident> AssignResident(
        Guid userId,
        ResidentRelationType relationType,
        bool livesHere,
        DateOnly? startDate)
    {
        if (userId == Guid.Empty)
        {
            return Error.Validation("ApartmentResident.UserRequired", "A user is required.");
        }

        if (_residents.Any(r => r.UserId == userId && r.IsActive))
        {
            return Error.Conflict(
                "ApartmentResident.AlreadyAssigned",
                "This user already has an active residency in this apartment.");
        }

        var resident = new ApartmentResident(
            Guid.NewGuid(),
            Id,
            TenantId,
            userId,
            relationType,
            livesHere,
            startDate);

        _residents.Add(resident);
        Raise(new ResidentAssignedToApartmentDomainEvent(Id, TenantId, resident.Id, userId));
        return resident;
    }

    // Ends an active residency (soft close): keeps the row for history, flips IsActive off.
    public Result EndResidency(Guid residentId, DateOnly endDate)
    {
        var resident = _residents.FirstOrDefault(r => r.Id == residentId);
        if (resident is null)
        {
            return Error.NotFound("ApartmentResident.NotFound", "Residency not found in this apartment.");
        }

        if (!resident.IsActive)
        {
            return Error.Conflict("ApartmentResident.AlreadyEnded", "The residency has already ended.");
        }

        resident.End(endDate);
        return Result.Success();
    }

    // Hard-removes a residency row (e.g. created in error).
    public Result RemoveResident(Guid residentId)
    {
        var resident = _residents.FirstOrDefault(r => r.Id == residentId);
        if (resident is null)
        {
            return Error.NotFound("ApartmentResident.NotFound", "Residency not found in this apartment.");
        }

        _residents.Remove(resident);
        Raise(new ResidentRemovedFromApartmentDomainEvent(Id, TenantId, resident.Id, resident.UserId));
        return Result.Success();
    }
}
