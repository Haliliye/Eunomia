namespace TodoApp.Domain.Teams;

/// <summary>
/// One column on a team's board. Key is the stable identifier UserStory.Status
/// actually stores and compares against — it never changes once created, so
/// renaming a column (editing Name) never touches existing stories' data,
/// and burndown/dashboard/CSV-import logic that compares against the literal
/// "Done" key keeps working even if a team relabels that column to
/// "Complete" or anything else. Any column, including the six seeded
/// defaults (ToDo/Analyze/Dev/Test/Debug/Done), can be removed as long as a
/// team keeps at least one — see Team.RemoveColumn for the trade-off of
/// removing "Done" specifically (several places key off that literal name
/// for sprint burndown/velocity and the dashboard's open/closed split).
/// </summary>
public class BoardColumn
{
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }

    private BoardColumn() { }

    public BoardColumn(string key, string name, int order)
    {
        Key = key;
        Name = name;
        Order = order;
    }

    public void Rename(string name) => Name = name;

    public void SetOrder(int order) => Order = order;
}
