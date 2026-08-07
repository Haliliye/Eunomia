namespace TodoApp.Domain.Teams;

/// <summary>
/// One column on a team's board. Key is the stable identifier UserStory.Status
/// actually stores and compares against — it never changes once created, so
/// renaming a column (editing Name) never touches existing stories' data,
/// and burndown/dashboard/CSV-import logic that compares against the literal
/// "Done" key keeps working even if a team relabels that column to
/// "Complete" or anything else. The six seeded columns (ToDo/Analyze/Dev/
/// Test/Debug/Done) can be renamed but not removed or have their Key
/// changed — see Team.RemoveColumn — since several places (recurring-story
/// completion, sprint burndown, dashboard counts) key specifically off the
/// literal "Done".
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
