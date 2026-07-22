namespace Dominodo.Operations.Contracts;

public sealed record RequestDetailDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Type,
    string? Category,
    string Title,
    string Description,
    string? Location,
    string Status,
    string Priority,
    Guid CreatedByUserId,
    Guid? ApartmentId,
    Guid? AssignedToUserId,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    string? Metadata,
    IReadOnlyList<RequestParticipantDto> Participants,
    IReadOnlyList<RequestUpdateDto> Updates,
    IReadOnlyList<RequestStatusHistoryDto> StatusHistory);

public sealed record RequestParticipantDto(
    Guid Id,
    Guid UserId,
    string ParticipantType,
    string Source,
    DateTimeOffset JoinedAtUtc);

public sealed record RequestUpdateDto(
    Guid Id,
    Guid AuthorUserId,
    string Type,
    string? Body,
    bool IsInternal,
    DateTimeOffset CreatedAtUtc);

public sealed record RequestStatusHistoryDto(
    Guid Id,
    string FromStatus,
    string ToStatus,
    Guid ChangedByUserId,
    DateTimeOffset ChangedAtUtc,
    string? Note);
