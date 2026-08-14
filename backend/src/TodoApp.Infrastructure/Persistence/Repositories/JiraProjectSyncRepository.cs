using MongoDB.Driver;
using TodoApp.Domain.Integrations;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class JiraProjectSyncRepository : IJiraProjectSyncRepository
{
    private readonly IMongoCollection<JiraProjectSyncDocument> _syncs;

    public JiraProjectSyncRepository(MongoDbContext context)
    {
        _syncs = context.JiraProjectSyncs;
    }

    public async Task<JiraProjectSync?> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var document = await _syncs.Find(s => s.TeamId == teamId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<JiraProjectSync>> GetAllAutoSyncEnabledAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _syncs.Find(s => s.AutoSyncEnabled).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task AddAsync(JiraProjectSync sync, CancellationToken cancellationToken = default) =>
        await _syncs.InsertOneAsync(ToDocument(sync), cancellationToken: cancellationToken);

    public async Task UpdateAsync(JiraProjectSync sync, CancellationToken cancellationToken = default) =>
        await _syncs.ReplaceOneAsync(s => s.Id == sync.Id, ToDocument(sync), cancellationToken: cancellationToken);

    private static JiraProjectSyncDocument ToDocument(JiraProjectSync sync) => new()
    {
        Id = sync.Id,
        TeamId = sync.TeamId,
        ProjectKey = sync.ProjectKey,
        ConnectedByUserId = sync.ConnectedByUserId,
        AutoSyncEnabled = sync.AutoSyncEnabled,
        LastSyncedOn = sync.LastSyncedOn,
        CreatedOn = sync.CreatedOn,
        History = sync.History.Select(h => new SyncLogEntryDocument
        {
            SyncedOn = h.SyncedOn,
            CreatedCount = h.CreatedCount,
            UpdatedCount = h.UpdatedCount,
            SkippedCount = h.SkippedCount,
        }).ToList(),
    };

    private static JiraProjectSync ToDomain(JiraProjectSyncDocument document) => JiraProjectSync.Rehydrate(
        document.Id, document.TeamId, document.ProjectKey, document.ConnectedByUserId,
        document.AutoSyncEnabled, document.LastSyncedOn, document.CreatedOn,
        document.History.Select(h => new SyncLogEntry(h.SyncedOn, h.CreatedCount, h.UpdatedCount, h.SkippedCount)));
}
