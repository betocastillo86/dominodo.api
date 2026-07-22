namespace Dominodo.Operations.Application.Abstractions;

// Produces the next per-tenant sequential value for a readable code (domain-model §5.2). Scope is
// (current tenant + prefix + year); the persistence adapter increments atomically. Value starts at 1.
// Codes are formatted by the caller, e.g. SOL-{year}-{value:D4} for requests, PAQ-… for deliveries.
public interface ISequenceProvider
{
    Task<int> NextAsync(string prefix, int year, CancellationToken cancellationToken = default);
}
