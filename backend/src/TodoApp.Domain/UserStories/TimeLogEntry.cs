namespace TodoApp.Domain.UserStories;

/// <summary>A single logged-time entry on a UserStory (US-138) — modeled like ChecklistItem/Attachment.</summary>
public class TimeLogEntry
{
    public string Id { get; private set; } = string.Empty;
    public double Hours { get; private set; }
    public string? Note { get; private set; }
    public string LoggedByUserId { get; private set; } = string.Empty;
    public DateTime LoggedOn { get; private set; }

    private TimeLogEntry() { }

    public TimeLogEntry(string id, double hours, string? note, string loggedByUserId, DateTime loggedOn)
    {
        Id = id;
        Hours = hours;
        Note = note;
        LoggedByUserId = loggedByUserId;
        LoggedOn = loggedOn;
    }
}
