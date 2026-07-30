namespace TodoApp.Domain.Teams;

/// <summary>
/// A reusable starting point for common story types (bug report, feature
/// request, tech debt) — owner-managed, like Label. Applying one is a
/// frontend-side convenience (pre-fill the create-story form, then add the
/// checklist items via the normal per-item endpoint) rather than a new
/// server-side "create from template" concept — keeps CreateUserStoryCommand
/// itself untouched.
/// </summary>
public class StoryTemplate
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? DefaultDescription { get; private set; }
    public string? DefaultPriority { get; private set; }
    private readonly List<string> _checklistItemTexts = new();
    public IReadOnlyList<string> ChecklistItemTexts => _checklistItemTexts.AsReadOnly();

    private StoryTemplate() { }

    public StoryTemplate(string id, string name, string? defaultDescription, string? defaultPriority, IEnumerable<string> checklistItemTexts)
    {
        Id = id;
        Name = name;
        DefaultDescription = defaultDescription;
        DefaultPriority = defaultPriority;
        _checklistItemTexts.AddRange(checklistItemTexts);
    }

    public void Update(string name, string? defaultDescription, string? defaultPriority, IEnumerable<string> checklistItemTexts)
    {
        Name = name;
        DefaultDescription = defaultDescription;
        DefaultPriority = defaultPriority;
        _checklistItemTexts.Clear();
        _checklistItemTexts.AddRange(checklistItemTexts);
    }
}
