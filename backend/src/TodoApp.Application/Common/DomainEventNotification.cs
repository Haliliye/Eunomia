using MediatR;
using TodoApp.Domain.Common;

namespace TodoApp.Application.Common;

/// <summary>
/// Wraps a plain Domain-layer IDomainEvent so it can be published through
/// MediatR. Domain stays free of the MediatR dependency; only Application
/// knows about it. Use this once you wire up dispatch (e.g. a repository or
/// unit-of-work that calls _mediator.Publish(new DomainEventNotification&lt;T&gt;(e))
/// for each aggregate's DomainEvents after a successful save).
/// </summary>
public class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
