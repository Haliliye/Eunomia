namespace TodoApp.Domain.Common;

/// <summary>
/// Base class for aggregate roots. An aggregate root is the only entry point
/// for modifying its aggregate, and it accumulates domain events raised
/// during its lifetime so the Application layer can dispatch them after
/// a successful persistence operation (e.g. via MediatR).
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }

    protected AggregateRoot(string id) : base(id) { }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
