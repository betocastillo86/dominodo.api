using Dominodo.Operations.Domain.Deliveries.Events;
using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Deliveries;

// A package/mail delivery (domain-model §3.2), an ITenantOwned aggregate registered by a vigilante and
// bound to a destination apartment. Code is a per-tenant readable sequence (PAQ-YYYY-NNNN). State changes
// only through the methods below, which guard the Received→Notified→Delivered/Returned flow.
public sealed class Delivery : AggregateRoot, ITenantOwned
{
    private Delivery() { } // EF Core

    private Delivery(
        Guid id,
        Guid tenantId,
        string code,
        Guid apartmentId,
        DeliveryType type,
        Guid registeredByUserId,
        DateTimeOffset receivedAtUtc,
        string? carrier,
        string? comment,
        string? photoUrl) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        ApartmentId = apartmentId;
        Type = type;
        RegisteredByUserId = registeredByUserId;
        ReceivedAtUtc = receivedAtUtc;
        Carrier = carrier;
        Comment = comment;
        PhotoUrl = photoUrl;
        Status = DeliveryStatus.Received;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = null!;
    public Guid ApartmentId { get; private set; }
    public DeliveryType Type { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public Guid RegisteredByUserId { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? Comment { get; private set; }
    public string? Carrier { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public string? ReceivedByName { get; private set; }
    public Guid? DeliveredToUserId { get; private set; }
    public string? Metadata { get; private set; }

    public static Result<Delivery> Register(
        Guid tenantId,
        string code,
        Guid apartmentId,
        DeliveryType type,
        Guid registeredByUserId,
        DateTimeOffset nowUtc,
        string? carrier = null,
        string? comment = null,
        string? photoUrl = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Error.Validation("Delivery.TenantRequired", "A tenant is required.");
        }

        if (apartmentId == Guid.Empty)
        {
            return Error.Validation("Delivery.ApartmentRequired", "A destination apartment is required.");
        }

        if (registeredByUserId == Guid.Empty)
        {
            return Error.Validation("Delivery.RegistrarRequired", "A registrar is required.");
        }

        var delivery = new Delivery(
            Guid.NewGuid(),
            tenantId,
            code,
            apartmentId,
            type,
            registeredByUserId,
            nowUtc,
            string.IsNullOrWhiteSpace(carrier) ? null : carrier.Trim(),
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim());

        delivery.Raise(new DeliveryRegisteredDomainEvent(
            delivery.Id, delivery.TenantId, delivery.Code, delivery.ApartmentId, registeredByUserId));

        return delivery;
    }

    public Result UpdateDetails(DeliveryType type, string? carrier, string? comment, string? photoUrl)
    {
        if (IsTerminal())
        {
            return Error.Conflict("Delivery.Terminal", "A delivered or returned delivery cannot be edited.");
        }

        Type = type;
        Carrier = string.IsNullOrWhiteSpace(carrier) ? null : carrier.Trim();
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
        return Result.Success();
    }

    public void SetMetadata(string? metadata) => Metadata = metadata;

    public Result MarkNotified()
    {
        if (Status != DeliveryStatus.Received)
        {
            return Error.Conflict("Delivery.InvalidTransition", $"Cannot notify a delivery in status {Status}.");
        }

        Status = DeliveryStatus.Notified;
        return Result.Success();
    }

    public Result MarkDelivered(DateTimeOffset nowUtc, string? receivedByName, Guid? deliveredToUserId)
    {
        if (Status is not (DeliveryStatus.Received or DeliveryStatus.Notified))
        {
            return Error.Conflict("Delivery.InvalidTransition", $"Cannot deliver a delivery in status {Status}.");
        }

        Status = DeliveryStatus.Delivered;
        DeliveredAtUtc = nowUtc;
        ReceivedByName = string.IsNullOrWhiteSpace(receivedByName) ? null : receivedByName.Trim();
        DeliveredToUserId = deliveredToUserId;

        Raise(new DeliveryDeliveredDomainEvent(Id, TenantId, ApartmentId, nowUtc, deliveredToUserId));
        return Result.Success();
    }

    public Result MarkReturned()
    {
        if (Status is not (DeliveryStatus.Received or DeliveryStatus.Notified))
        {
            return Error.Conflict("Delivery.InvalidTransition", $"Cannot return a delivery in status {Status}.");
        }

        Status = DeliveryStatus.Returned;
        return Result.Success();
    }

    private bool IsTerminal() => Status is DeliveryStatus.Delivered or DeliveryStatus.Returned;
}
