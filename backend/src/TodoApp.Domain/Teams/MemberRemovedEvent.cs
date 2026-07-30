using TodoApp.Domain.Common;

namespace TodoApp.Domain.Teams;

public sealed class MemberRemovedEvent : IDomainEvent
{
    public string TeamId { get; }
    public string RemovedUserId { get; }
    public DateTime OccurredOn { get; }

    public MemberRemovedEvent(string teamId, string removedUserId)
    {
        TeamId = teamId;
        RemovedUserId = removedUserId;
        OccurredOn = DateTime.UtcNow;
    }
}
