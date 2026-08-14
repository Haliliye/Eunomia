namespace TodoApp.Infrastructure.Persistence.Documents;

public class SyncLogEntryDocument
{
    public DateTime SyncedOn { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
}
