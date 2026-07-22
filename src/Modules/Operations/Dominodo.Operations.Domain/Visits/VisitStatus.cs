namespace Dominodo.Operations.Domain.Visits;

// Lifecycle (domain-model §3.3): InProgress → Finished.
public enum VisitStatus
{
    InProgress,
    Finished
}
