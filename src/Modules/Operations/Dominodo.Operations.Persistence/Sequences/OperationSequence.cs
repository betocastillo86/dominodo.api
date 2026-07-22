namespace Dominodo.Operations.Persistence.Sequences;

// Persistence-only counter backing readable codes (domain-model §5.2). NOT a domain aggregate — it is
// the storage detail the domain deliberately leaves to persistence. One row per (TenantId, Prefix, Year);
// SequenceProvider increments Value atomically.
internal sealed class OperationSequence
{
    public Guid TenantId { get; set; }
    public string Prefix { get; set; } = null!;
    public int Year { get; set; }
    public int Value { get; set; }
}
