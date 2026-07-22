namespace Dominodo.Operations.Domain.Requests;

// The request timeline (domain-model §3.1.2): progress, comments, evidence, resolutions. Child entity
// under the Request aggregate — only mutated through Request. IsInternal marks a staff-only note.
public sealed class RequestUpdate
{
    private RequestUpdate() { } // EF Core

    internal RequestUpdate(
        Guid id,
        Guid requestId,
        Guid authorUserId,
        RequestUpdateType type,
        string? body,
        bool isInternal,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        RequestId = requestId;
        AuthorUserId = authorUserId;
        Type = type;
        Body = body;
        IsInternal = isInternal;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public RequestUpdateType Type { get; private set; }
    public string? Body { get; private set; }
    public bool IsInternal { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
