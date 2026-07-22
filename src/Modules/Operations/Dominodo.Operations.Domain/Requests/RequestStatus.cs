namespace Dominodo.Operations.Domain.Requests;

// Lifecycle (domain-model §3.1):
//   New → InReview → InProgress → Resolved → Closed
//   New/InReview/InProgress → Rejected | Cancelled
//   Resolved → Reopened → InProgress
public enum RequestStatus
{
    New,
    InReview,
    InProgress,
    Resolved,
    Closed,
    Rejected,
    Cancelled,
    Reopened
}
