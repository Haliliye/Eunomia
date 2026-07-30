using TodoApp.Domain.Common;

namespace TodoApp.Domain.PersonalTasks;

/// <summary>
/// A private to-do outside any team (US-140/141/142) — its own aggregate
/// root (unlike UserStory, which always belongs to a Team) since it has an
/// entirely independent lifecycle and visibility (only its owner ever sees it).
/// </summary>
public class PersonalTask : AggregateRoot
{
    public string OwnerUserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime? DueDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedOn { get; private set; }

    /// <summary>Set once this task has been converted to a real team UserStory (US-141) — kept for history rather than hard-deleting the personal task.</summary>
    public string? ConvertedToUserStoryId { get; private set; }

    private PersonalTask() { }

    private PersonalTask(string id, string ownerUserId, string title, string? description, DateTime? dueDate) : base(id)
    {
        OwnerUserId = ownerUserId;
        Title = title;
        Description = description;
        DueDate = dueDate;
        CreatedOn = DateTime.UtcNow;
    }

    public static PersonalTask Create(string id, string ownerUserId, string title, string? description, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        return new PersonalTask(id, ownerUserId, title.Trim(), description?.Trim(), dueDate);
    }

    public static PersonalTask Rehydrate(
        string id, string ownerUserId, string title, string? description, DateTime? dueDate,
        bool isCompleted, DateTime createdOn, string? convertedToUserStoryId)
    {
        var task = new PersonalTask(id, ownerUserId, title, description, dueDate)
        {
            IsCompleted = isCompleted,
            CreatedOn = createdOn,
            ConvertedToUserStoryId = convertedToUserStoryId
        };
        return task;
    }

    public void Update(string title, string? description, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        EnsureNotConverted();
        Title = title.Trim();
        Description = description?.Trim();
        DueDate = dueDate;
    }

    public void SetCompleted(bool isCompleted)
    {
        EnsureNotConverted();
        IsCompleted = isCompleted;
    }

    /// <summary>US-141: "the personal task is removed (or marked converted)" — marked, so the
    /// person keeps a record of what they converted and can still see it in their history.</summary>
    public void MarkConverted(string userStoryId)
    {
        EnsureNotConverted();
        ConvertedToUserStoryId = userStoryId;
    }

    private void EnsureNotConverted()
    {
        if (ConvertedToUserStoryId is not null)
            throw new InvalidOperationException("This task was already converted to a team user story.");
    }
}
