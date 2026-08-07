using MongoDB.Bson.Serialization.Attributes;

namespace TodoApp.Infrastructure.Persistence.Documents;

/// <summary>
/// Plain persistence shape for the Team aggregate — simple public
/// get/set properties so MongoDB.Driver's conventions (serialization AND
/// LINQ filter translation) can work with it directly. The Team aggregate
/// itself stays free of any MongoDB attributes; TeamRepository maps
/// between the two.
/// </summary>
public class TeamDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<TeamMemberDocument> Members { get; set; } = new();
    public List<LabelDocument> Labels { get; set; } = new();
    public List<ColumnWipLimitDocument> WipLimits { get; set; } = new();
    public List<StoryTemplateDocument> Templates { get; set; } = new();
    public List<BoardColumnDocument> Columns { get; set; } = new();
}

public class BoardColumnDocument
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class StoryTemplateDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DefaultDescription { get; set; }
    public string? DefaultPriority { get; set; }
    public List<string> ChecklistItemTexts { get; set; } = new();
}

public class ColumnWipLimitDocument
{
    public string Status { get; set; } = string.Empty;
    public int Limit { get; set; }
}

public class TeamMemberDocument
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedOn { get; set; }
}

public class LabelDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
