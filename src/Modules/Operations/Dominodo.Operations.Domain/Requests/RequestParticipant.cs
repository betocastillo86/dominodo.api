namespace Dominodo.Operations.Domain.Requests;

// Who is "in" a request and receives its updates (domain-model §3.1.2). Child entity under the Request
// aggregate — only mutated through Request. UserId is a raw Guid from Users (NO cross-module FK).
// Unique (RequestId, UserId).
public sealed class RequestParticipant
{
    private RequestParticipant() { } // EF Core

    internal RequestParticipant(
        Guid id,
        Guid requestId,
        Guid userId,
        ParticipantType participantType,
        ParticipantSource source,
        DateTimeOffset joinedAtUtc)
    {
        Id = id;
        RequestId = requestId;
        UserId = userId;
        ParticipantType = participantType;
        Source = source;
        JoinedAtUtc = joinedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid UserId { get; private set; }
    public ParticipantType ParticipantType { get; private set; }
    public ParticipantSource Source { get; private set; }
    public DateTimeOffset JoinedAtUtc { get; private set; }
}
