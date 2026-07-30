namespace TodoApp.Domain.UserStories;

/// <summary>
/// Expanded from the original ToDo/InProgress/Done into a more realistic dev
/// workflow — "In Progress" was a catch-all that these stages subdivide.
/// See UserStory.ChangeStatus for the allowed transitions between them.
/// </summary>
public enum UserStoryStatus
{
    ToDo,
    Analyze,
    Dev,
    Test,
    Debug,
    Done
}

public enum UserStoryPriority
{
    Low = 4,
    Medium = 3,
    High = 2,
    Critical = 1
}
