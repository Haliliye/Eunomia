using TodoApp.Domain.Common;

namespace TodoApp.Domain.Integrations;

/// <summary>
/// Tracks that a team was imported from (or is linked to) a specific Azure DevOps
/// project — created/refreshed automatically every time ImportFromAzureDevOpsCommand
/// or CreateTeamFromAzureDevOpsCommand runs. AutoSyncEnabled is opt-in (defaults
/// false): AzureDevOpsAutoSyncBackgroundService only re-imports teams where this is
/// explicitly true, since silently pulling in Azure DevOps changes on a schedule is
/// a bigger behavioral change than a one-off manual import and shouldn't
/// happen without the person asking for it.
/// </summary>
public class AzureDevOpsProjectSync : AggregateRoot
{
    private const int MaxHistoryEntries = 10;

    public string TeamId { get; private set; } = string.Empty;
    public string ProjectName { get; private set; } = string.Empty;

    /// <summary>Whose Azure DevOps connection (access/refresh tokens) the background sync uses — the person who set this team up, or last re-confirmed it.</summary>
    public string ConnectedByUserId { get; private set; } = string.Empty;

    public bool AutoSyncEnabled { get; private set; }
    public DateTime? LastSyncedOn { get; private set; }
    public DateTime CreatedOn { get; private set; }

    private readonly List<SyncLogEntry> _history = new();
    /// <summary>Most recent first — capped at MaxHistoryEntries, since this is a "what happened lately" panel, not a full audit log.</summary>
    public IReadOnlyList<SyncLogEntry> History => _history.AsReadOnly();

    private AzureDevOpsProjectSync() { }

    private AzureDevOpsProjectSync(string id, string teamId, string projectKey, string connectedByUserId) : base(id)
    {
        TeamId = teamId;
        ProjectName = projectKey;
        ConnectedByUserId = connectedByUserId;
        CreatedOn = DateTime.UtcNow;
    }

    public static AzureDevOpsProjectSync Create(string id, string teamId, string projectKey, string connectedByUserId) =>
        new(id, teamId, projectKey, connectedByUserId);

    public static AzureDevOpsProjectSync Rehydrate(string id, string teamId, string projectKey, string connectedByUserId, bool autoSyncEnabled, DateTime? lastSyncedOn, DateTime createdOn, IEnumerable<SyncLogEntry>? history = null)
    {
        var sync = new AzureDevOpsProjectSync(id, teamId, projectKey, connectedByUserId)
        {
            AutoSyncEnabled = autoSyncEnabled,
            LastSyncedOn = lastSyncedOn,
            CreatedOn = createdOn
        };
        if (history is not null) sync._history.AddRange(history);
        return sync;
    }

    public void SetAutoSync(bool enabled, string connectedByUserId)
    {
        AutoSyncEnabled = enabled;
        ConnectedByUserId = connectedByUserId; // re-confirm whose token to use, in case ownership changed
    }

    /// <summary>Replaces the old no-argument MarkSynced() — records what an import/sync actually did, not just that one happened, so the sync panel can show "12 created, 4 updated" instead of just a timestamp.</summary>
    public void RecordSync(int createdCount, int updatedCount, int skippedCount)
    {
        LastSyncedOn = DateTime.UtcNow;
        _history.Insert(0, new SyncLogEntry(LastSyncedOn.Value, createdCount, updatedCount, skippedCount));
        if (_history.Count > MaxHistoryEntries)
            _history.RemoveRange(MaxHistoryEntries, _history.Count - MaxHistoryEntries);
    }
}
