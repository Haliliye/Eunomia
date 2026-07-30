namespace TodoApp.Domain.UserStories;

/// <summary>
/// A single checklist entry on a UserStory (US-122/123/124). Modeled as part
/// of the UserStory aggregate — like TeamMember is part of Team — rather than
/// its own aggregate, since a checklist item has no meaning outside its parent
/// story and the two are always loaded/saved together.
/// </summary>
public class ChecklistItem
{
    public string Id { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; }
    public int Order { get; private set; }

    private ChecklistItem() { }

    public ChecklistItem(string id, string text, int order)
    {
        Id = id;
        Text = text;
        Order = order;
    }

    public static ChecklistItem Rehydrate(string id, string text, bool isCompleted, int order) =>
        new(id, text, order) { IsCompleted = isCompleted };

    public void Toggle() => IsCompleted = !IsCompleted;
    public void SetOrder(int order) => Order = order;
}
