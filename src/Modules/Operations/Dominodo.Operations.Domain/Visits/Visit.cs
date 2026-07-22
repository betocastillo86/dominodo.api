using Dominodo.Operations.Domain.Visits.Events;
using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Visits;

// A visit / entry log (domain-model §3.3), an ITenantOwned aggregate registered by a vigilante and bound
// to a destination apartment. No readable Code (unlike Request/Delivery). State changes only through the
// methods below, which guard the InProgress → Finished flow.
public sealed class Visit : AggregateRoot, ITenantOwned
{
    private Visit() { } // EF Core

    private Visit(
        Guid id,
        Guid tenantId,
        Guid apartmentId,
        VisitType type,
        string visitorName,
        Guid registeredByUserId,
        DateTimeOffset entryAtUtc,
        string? visitorDocument,
        string? photoUrl,
        string? vehiclePlate,
        Guid? authorizedByUserId) : base(id)
    {
        TenantId = tenantId;
        ApartmentId = apartmentId;
        Type = type;
        VisitorName = visitorName;
        RegisteredByUserId = registeredByUserId;
        EntryAtUtc = entryAtUtc;
        VisitorDocument = visitorDocument;
        PhotoUrl = photoUrl;
        VehiclePlate = vehiclePlate;
        AuthorizedByUserId = authorizedByUserId;
        Status = VisitStatus.InProgress;
    }

    public Guid TenantId { get; private set; }
    public Guid ApartmentId { get; private set; }
    public VisitType Type { get; private set; }
    public VisitStatus Status { get; private set; }
    public string VisitorName { get; private set; } = null!;
    public string? VisitorDocument { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? VehiclePlate { get; private set; }
    public decimal? AmountPaid { get; private set; }
    public Guid RegisteredByUserId { get; private set; }
    public Guid? AuthorizedByUserId { get; private set; }
    public DateTimeOffset EntryAtUtc { get; private set; }
    public DateTimeOffset? ExitAtUtc { get; private set; }
    public string? Metadata { get; private set; }

    public static Result<Visit> Register(
        Guid tenantId,
        Guid apartmentId,
        VisitType type,
        string visitorName,
        Guid registeredByUserId,
        DateTimeOffset nowUtc,
        string? visitorDocument = null,
        string? photoUrl = null,
        string? vehiclePlate = null,
        Guid? authorizedByUserId = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Error.Validation("Visit.TenantRequired", "A tenant is required.");
        }

        if (apartmentId == Guid.Empty)
        {
            return Error.Validation("Visit.ApartmentRequired", "A destination apartment is required.");
        }

        if (registeredByUserId == Guid.Empty)
        {
            return Error.Validation("Visit.RegistrarRequired", "A registrar is required.");
        }

        if (string.IsNullOrWhiteSpace(visitorName))
        {
            return Error.Validation("Visit.VisitorNameRequired", "A visitor name is required.");
        }

        var visit = new Visit(
            Guid.NewGuid(),
            tenantId,
            apartmentId,
            type,
            visitorName.Trim(),
            registeredByUserId,
            nowUtc,
            string.IsNullOrWhiteSpace(visitorDocument) ? null : visitorDocument.Trim(),
            string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim(),
            string.IsNullOrWhiteSpace(vehiclePlate) ? null : vehiclePlate.Trim(),
            authorizedByUserId);

        visit.Raise(new VisitRegisteredDomainEvent(
            visit.Id, visit.TenantId, visit.ApartmentId, registeredByUserId));

        return visit;
    }

    public Result UpdateDetails(
        VisitType type,
        string visitorName,
        string? visitorDocument,
        string? photoUrl,
        string? vehiclePlate)
    {
        if (Status == VisitStatus.Finished)
        {
            return Error.Conflict("Visit.Finished", "A finished visit cannot be edited.");
        }

        if (string.IsNullOrWhiteSpace(visitorName))
        {
            return Error.Validation("Visit.VisitorNameRequired", "A visitor name is required.");
        }

        Type = type;
        VisitorName = visitorName.Trim();
        VisitorDocument = string.IsNullOrWhiteSpace(visitorDocument) ? null : visitorDocument.Trim();
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
        VehiclePlate = string.IsNullOrWhiteSpace(vehiclePlate) ? null : vehiclePlate.Trim();
        return Result.Success();
    }

    public void SetMetadata(string? metadata) => Metadata = metadata;

    public Result Finish(DateTimeOffset nowUtc, decimal? amountPaid)
    {
        if (Status == VisitStatus.Finished)
        {
            return Error.Conflict("Visit.AlreadyFinished", "The visit has already finished.");
        }

        if (amountPaid is < 0)
        {
            return Error.Validation("Visit.InvalidAmount", "The amount paid cannot be negative.");
        }

        Status = VisitStatus.Finished;
        ExitAtUtc = nowUtc;
        AmountPaid = amountPaid;
        return Result.Success();
    }
}
