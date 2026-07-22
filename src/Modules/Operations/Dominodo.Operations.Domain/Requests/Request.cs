using Dominodo.Operations.Domain.Requests.Events;
using Dominodo.Shared.Kernel;

namespace Dominodo.Operations.Domain.Requests;

// A PQRS request (domain-model §3.1), an ITenantOwned aggregate. Every read is scoped by TenantId
// (ForCurrentTenant, doc 09). Code is a per-tenant readable sequence (SOL-YYYY-NNNN) assigned at
// creation. State changes only through the methods below, which enforce the lifecycle graph and append
// a RequestStatusHistory row per transition.
public sealed class Request : AggregateRoot, ITenantOwned
{
    private readonly List<RequestParticipant> _participants = new();
    private readonly List<RequestUpdate> _updates = new();
    private readonly List<RequestAttachment> _attachments = new();
    private readonly List<RequestStatusHistory> _statusHistory = new();

    private Request() { } // EF Core

    private Request(
        Guid id,
        Guid tenantId,
        string code,
        RequestType type,
        string title,
        string description,
        RequestPriority priority,
        Guid createdByUserId,
        Guid? apartmentId,
        string? category,
        string? location) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Type = type;
        Title = title;
        Description = description;
        Priority = priority;
        CreatedByUserId = createdByUserId;
        ApartmentId = apartmentId;
        Category = category;
        Location = location;
        Status = RequestStatus.New;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = null!;
    public RequestType Type { get; private set; }
    public string? Category { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? Location { get; private set; }
    public RequestStatus Status { get; private set; }
    public RequestPriority Priority { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? ApartmentId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string? Metadata { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<RequestParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<RequestUpdate> Updates => _updates.AsReadOnly();
    public IReadOnlyCollection<RequestAttachment> Attachments => _attachments.AsReadOnly();
    public IReadOnlyCollection<RequestStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    // Opens a request and seeds the reporter as its first participant (domain-model §3.1.1).
    public static Result<Request> Open(
        Guid tenantId,
        string code,
        RequestType type,
        string title,
        string description,
        RequestPriority priority,
        Guid createdByUserId,
        DateTimeOffset nowUtc,
        Guid? apartmentId = null,
        string? category = null,
        string? location = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Error.Validation("Request.TenantRequired", "A tenant is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            return Error.Validation("Request.ReporterRequired", "A reporter is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("Request.TitleRequired", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Error.Validation("Request.DescriptionRequired", "Description is required.");
        }

        var request = new Request(
            Guid.NewGuid(),
            tenantId,
            code,
            type,
            title.Trim(),
            description.Trim(),
            priority,
            createdByUserId,
            apartmentId,
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            string.IsNullOrWhiteSpace(location) ? null : location.Trim());

        request._participants.Add(new RequestParticipant(
            Guid.NewGuid(),
            request.Id,
            createdByUserId,
            ParticipantType.Reporter,
            ParticipantSource.Self,
            nowUtc));

        request.Raise(new RequestOpenedDomainEvent(
            request.Id, request.TenantId, request.Code, createdByUserId, null));

        return request;
    }

    public Result Update(
        string title,
        string description,
        RequestType type,
        RequestPriority priority,
        string? category,
        string? location)
    {
        if (IsTerminal())
        {
            return Error.Conflict("Request.Terminal", "A closed, rejected or cancelled request cannot be edited.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("Request.TitleRequired", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Error.Validation("Request.DescriptionRequired", "Description is required.");
        }

        Title = title.Trim();
        Description = description.Trim();
        Type = type;
        Priority = priority;
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        return Result.Success();
    }

    public void SetMetadata(string? metadata) => Metadata = metadata;

    public Result Assign(Guid assigneeUserId)
    {
        if (assigneeUserId == Guid.Empty)
        {
            return Error.Validation("Request.AssigneeRequired", "An assignee is required.");
        }

        if (IsTerminal())
        {
            return Error.Conflict("Request.Terminal", "A closed, rejected or cancelled request cannot be assigned.");
        }

        AssignedToUserId = assigneeUserId;
        return Result.Success();
    }

    // Drives the lifecycle graph. Appends a RequestStatusHistory row and raises
    // RequestStatusChangedDomainEvent; a transition into Closed also raises RequestClosedDomainEvent.
    public Result ChangeStatus(RequestStatus target, Guid changedByUserId, DateTimeOffset nowUtc, string? note = null)
    {
        if (target == Status)
        {
            return Error.Conflict("Request.SameStatus", "The request is already in that status.");
        }

        if (!IsTransitionAllowed(Status, target))
        {
            return Error.Conflict(
                "Request.InvalidTransition",
                $"Cannot move a request from {Status} to {target}.");
        }

        var from = Status;
        Status = target;

        if (target == RequestStatus.Resolved)
        {
            ResolvedAtUtc = nowUtc;
        }

        if (target == RequestStatus.Closed)
        {
            ClosedAtUtc = nowUtc;
        }

        _statusHistory.Add(new RequestStatusHistory(
            Guid.NewGuid(), Id, from, target, changedByUserId, nowUtc, note));

        Raise(new RequestStatusChangedDomainEvent(Id, TenantId, from, target, changedByUserId));

        if (target == RequestStatus.Closed)
        {
            Raise(new RequestClosedDomainEvent(Id, TenantId, ClosedAtUtc!.Value));
        }

        return Result.Success();
    }

    // Adds a timeline entry (progress/comment/evidence/resolution) and raises RequestUpdatedDomainEvent.
    public Result<RequestUpdate> AddUpdate(
        Guid authorUserId,
        RequestUpdateType type,
        string? body,
        bool isInternal,
        DateTimeOffset nowUtc)
    {
        if (authorUserId == Guid.Empty)
        {
            return Error.Validation("Request.AuthorRequired", "An author is required.");
        }

        if (IsTerminal())
        {
            return Error.Conflict("Request.Terminal", "A closed, rejected or cancelled request cannot receive updates.");
        }

        var update = new RequestUpdate(
            Guid.NewGuid(),
            Id,
            authorUserId,
            type,
            string.IsNullOrWhiteSpace(body) ? null : body.Trim(),
            isInternal,
            nowUtc);

        _updates.Add(update);
        Raise(new RequestUpdatedDomainEvent(Id, TenantId, update.Id, authorUserId));
        return update;
    }

    // Adds a participant (follower or auto-matched reporter). Unique per (RequestId, UserId).
    public Result<RequestParticipant> AddParticipant(
        Guid userId,
        ParticipantType participantType,
        ParticipantSource source,
        DateTimeOffset nowUtc)
    {
        if (userId == Guid.Empty)
        {
            return Error.Validation("RequestParticipant.UserRequired", "A user is required.");
        }

        if (_participants.Any(p => p.UserId == userId))
        {
            return Error.Conflict(
                "RequestParticipant.AlreadyParticipant",
                "This user is already a participant in the request.");
        }

        var participant = new RequestParticipant(
            Guid.NewGuid(), Id, userId, participantType, source, nowUtc);

        _participants.Add(participant);
        return participant;
    }

    public bool IsParticipant(Guid userId) => _participants.Any(p => p.UserId == userId);

    private bool IsTerminal() =>
        Status is RequestStatus.Closed or RequestStatus.Rejected or RequestStatus.Cancelled;

    private static bool IsTransitionAllowed(RequestStatus from, RequestStatus to) => from switch
    {
        RequestStatus.New => to is RequestStatus.InReview or RequestStatus.InProgress
            or RequestStatus.Rejected or RequestStatus.Cancelled,
        RequestStatus.InReview => to is RequestStatus.InProgress
            or RequestStatus.Rejected or RequestStatus.Cancelled,
        RequestStatus.InProgress => to is RequestStatus.Resolved
            or RequestStatus.Rejected or RequestStatus.Cancelled,
        RequestStatus.Resolved => to is RequestStatus.Closed or RequestStatus.Reopened,
        RequestStatus.Reopened => to is RequestStatus.InProgress
            or RequestStatus.Rejected or RequestStatus.Cancelled,
        _ => false,
    };
}
