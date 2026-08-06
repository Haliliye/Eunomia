namespace TodoApp.Infrastructure.Persistence.Documents;

public class JiraProjectSyncDocument
{
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string ConnectedByUserId { get; set; } = string.Empty;
    public bool AutoSyncEnabled { get; set; }
    public DateTime? LastSyncedOn { get; set; }
    public DateTime CreatedOn { get; set; }
}
