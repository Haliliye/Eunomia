namespace TodoApp.Infrastructure.Persistence.Documents;

public class AzureDevOpsProjectSyncDocument
{
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ConnectedByUserId { get; set; } = string.Empty;
    public bool AutoSyncEnabled { get; set; }
    public DateTime? LastSyncedOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public List<SyncLogEntryDocument> History { get; set; } = new();
}
