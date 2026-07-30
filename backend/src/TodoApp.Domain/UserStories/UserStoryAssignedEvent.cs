using TodoApp.Domain.Common;

namespace TodoApp.Domain.UserStories;

public sealed class UserStoryAssignedEvent : IDomainEvent
{
    public string UserStoryId { get; }
    public string Title { get; }
    public string AssigneeId { get; }
    public DateTime OccurredOn { get; }

    public UserStoryAssignedEvent(string userStoryId, string title, string assigneeId)
    {
        UserStoryId = userStoryId;
        Title = title;
        AssigneeId = assigneeId;
        OccurredOn = DateTime.UtcNow;
    }
}
