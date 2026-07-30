namespace TodoApp.Domain.Teams;

/// <summary>
/// A team-scoped label (US-125) — modeled inside the Team aggregate, like
/// TeamMember, since a label has no meaning or lifecycle outside its team.
/// Applied to stories by reference (UserStory.LabelIds) rather than embedding
/// the label's own data on every story, so renaming/recoloring a label
/// updates everywhere it's used without touching any story.
/// </summary>
public class Label
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;

    private Label() { }

    public Label(string id, string name, string color)
    {
        Id = id;
        Name = name;
        Color = color;
    }

    public void Update(string name, string color)
    {
        Name = name;
        Color = color;
    }
}
