using MediatR;
using TodoApp.Domain.Common;

namespace TodoApp.Application.Common;

/// <summary>
/// Publishes an aggregate's pending domain events through MediatR (wrapped in
/// DomainEventNotification&lt;T&gt;) and clears them. Call this after a
/// successful repository save — it's the missing piece that turns the
/// Domain/IDomainEvent + DomainEventNotification&lt;T&gt; bridge (which
/// existed but was never wired up) into something that actually dispatches.
/// </summary>
public static class DomainEventDispatchExtensions
{
    public static async Task PublishDomainEventsAsync(
        this IMediator mediator, AggregateRoot aggregate, CancellationToken cancellationToken = default)
    {
        var events = aggregate.DomainEvents.ToList();
        aggregate.ClearDomainEvents();

        foreach (var domainEvent in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
            await mediator.Publish(notification, cancellationToken);
        }
    }
}
