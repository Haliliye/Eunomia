namespace TodoApp.Domain.Integrations;

/// <summary>
/// One row of an integration's sync history — embedded on JiraProjectSync/
/// AzureDevOpsProjectSync rather than its own collection, since it's only
/// ever read alongside its parent sync record and a handful of entries is
/// all a "what happened last time" panel needs.
/// </summary>
public class SyncLogEntry
{
    public DateTime SyncedOn { get; private set; }
    public int CreatedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int SkippedCount { get; private set; }

    private SyncLogEntry() { }

    public SyncLogEntry(DateTime syncedOn, int createdCount, int updatedCount, int skippedCount)
    {
        SyncedOn = syncedOn;
        CreatedCount = createdCount;
        UpdatedCount = updatedCount;
        SkippedCount = skippedCount;
    }
}
