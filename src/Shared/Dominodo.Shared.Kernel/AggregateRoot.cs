namespace Dominodo.Shared.Kernel;

public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id) : base(id) { }
    protected AggregateRoot() { }
}
