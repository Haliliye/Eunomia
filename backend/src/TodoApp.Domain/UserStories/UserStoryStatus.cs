namespace TodoApp.Domain.UserStories;

// The old fixed ToDo/Analyze/Dev/Test/Debug/Done enum was replaced by
// per-team, customizable board columns (see TodoApp.Domain.Teams.BoardColumn)
// — UserStory.Status is now a plain string holding a column's Key. The six
// original values still exist as every team's seeded default columns and
// their Keys are unchanged, so no data migration was needed; this file is
// kept only for UserStoryPriority, which is still a fixed enum.

public enum UserStoryPriority
{
    Low = 4,
    Medium = 3,
    High = 2,
    Critical = 1
}
