using TodoApp.Domain.Common;

namespace TodoApp.Domain.Teams;

public sealed class TeamCreatedEvent : IDomainEvent
{
    public string TeamId { get; }
    public string OwnerId { get; }
    public DateTime OccurredOn { get; }

    public TeamCreatedEvent(string teamId, string ownerId)
    {
        TeamId = teamId;
        OwnerId = ownerId;
        OccurredOn = DateTime.UtcNow;
    }
}
