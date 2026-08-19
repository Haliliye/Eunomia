using MongoDB.Bson.Serialization.Attributes;

namespace TodoApp.Infrastructure.Persistence.Documents;

/// <summary>
/// Plain persistence shape for the UserStory aggregate — see TeamDocument
/// for why this exists separately from the domain type.
/// </summary>
public class UserStoryDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedOn { get; set; }
    public int Version { get; set; }
    public bool IsArchived { get; set; }
    public int? StoryPoints { get; set; }
    public string? SprintId { get; set; }
    public DateTime? ReminderSentOn { get; set; }
    public string? RecurrenceFrequency { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public List<string> LabelIds { get; set; } = new();
    public List<ChecklistItemDocument> ChecklistItems { get; set; } = new();
    public List<AttachmentDocument> Attachments { get; set; } = new();
    public double? EstimatedHours { get; set; }
    public List<TimeLogEntryDocument> TimeLogEntries { get; set; } = new();
    public List<StoryLinkDocument> Links { get; set; } = new();
    public string? CreatedByUserId { get; set; }
    public string? ParentId { get; set; }
    public string? JiraIssueKey { get; set; }
    public string? EpicId { get; set; }
    public string? AzureDevOpsWorkItemId { get; set; }
    public string? GitHubIssueKey { get; set; }
}

public class StoryLinkDocument
{
    public string LinkedStoryId { get; set; } = string.Empty;
    public string LinkType { get; set; } = string.Empty;
}

public class TimeLogEntryDocument
{
    public string Id { get; set; } = string.Empty;
    public double Hours { get; set; }
    public string? Note { get; set; }
    public string LoggedByUserId { get; set; } = string.Empty;
    public DateTime LoggedOn { get; set; }
}

public class ChecklistItemDocument
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Order { get; set; }
}

public class AttachmentDocument
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTime UploadedOn { get; set; }
}
