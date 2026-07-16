namespace Dominodo.Shared.Kernel;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected Entity(Guid id) => Id = id;
    protected Entity() { }

    public Guid Id { get; protected init; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => (GetType(), Id).GetHashCode();
}
