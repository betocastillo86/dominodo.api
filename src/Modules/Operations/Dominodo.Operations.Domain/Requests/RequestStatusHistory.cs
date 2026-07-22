namespace Dominodo.Operations.Domain.Requests;

// Timeline of status transitions (domain-model §3.1.2) — queryable, one row per transition. Child
// entity under the Request aggregate — only appended through Request.
public sealed class RequestStatusHistory
{
    private RequestStatusHistory() { } // EF Core

    internal RequestStatusHistory(
        Guid id,
        Guid requestId,
        RequestStatus fromStatus,
        RequestStatus toStatus,
        Guid changedByUserId,
        DateTimeOffset changedAtUtc,
        string? note)
    {
        Id = id;
        RequestId = requestId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        ChangedAtUtc = changedAtUtc;
        Note = note;
    }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public RequestStatus FromStatus { get; private set; }
    public RequestStatus ToStatus { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public string? Note { get; private set; }
}
