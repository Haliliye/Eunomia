namespace TodoApp.Domain.Common;

/// <summary>
/// Marker interface for domain events. Deliberately has zero dependency on
/// MediatR or anything else outside the Domain project — the Domain layer
/// must not know how its events get dispatched. The Application layer
/// bridges these into MediatR notifications (see
/// TodoApp.Application.Common.DomainEventNotification&lt;T&gt;) when it's
/// ready to publish them after a successful save.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
