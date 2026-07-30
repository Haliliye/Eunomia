namespace TodoApp.Domain.Teams;

/// <summary>
/// An optional per-column work-in-progress cap on the board (classic Kanban
/// discipline) — owner-configurable, and entirely optional: a column with no
/// entry here just has no limit, same as before this feature existed. Status
/// is stored as a plain string rather than referencing UserStoryStatus
/// directly, keeping the Teams and UserStories bounded contexts decoupled.
/// </summary>
public class ColumnWipLimit
{
    public string Status { get; private set; } = string.Empty;
    public int Limit { get; private set; }

    private ColumnWipLimit() { }

    public ColumnWipLimit(string status, int limit)
    {
        Status = status;
        Limit = limit;
    }

    public void SetLimit(int limit) => Limit = limit;
}
