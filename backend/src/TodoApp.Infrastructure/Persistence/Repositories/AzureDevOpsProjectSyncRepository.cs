using MongoDB.Driver;
using TodoApp.Domain.Integrations;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class AzureDevOpsProjectSyncRepository : IAzureDevOpsProjectSyncRepository
{
    private readonly IMongoCollection<AzureDevOpsProjectSyncDocument> _syncs;

    public AzureDevOpsProjectSyncRepository(MongoDbContext context)
    {
        _syncs = context.AzureDevOpsProjectSyncs;
    }

    public async Task<AzureDevOpsProjectSync?> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var document = await _syncs.Find(s => s.TeamId == teamId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<AzureDevOpsProjectSync>> GetAllAutoSyncEnabledAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _syncs.Find(s => s.AutoSyncEnabled).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task AddAsync(AzureDevOpsProjectSync sync, CancellationToken cancellationToken = default) =>
        await _syncs.InsertOneAsync(ToDocument(sync), cancellationToken: cancellationToken);

    public async Task UpdateAsync(AzureDevOpsProjectSync sync, CancellationToken cancellationToken = default) =>
        await _syncs.ReplaceOneAsync(s => s.Id == sync.Id, ToDocument(sync), cancellationToken: cancellationToken);

    private static AzureDevOpsProjectSyncDocument ToDocument(AzureDevOpsProjectSync sync) => new()
    {
        Id = sync.Id,
        TeamId = sync.TeamId,
        ProjectName = sync.ProjectName,
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

    private static AzureDevOpsProjectSync ToDomain(AzureDevOpsProjectSyncDocument document) => AzureDevOpsProjectSync.Rehydrate(
        document.Id, document.TeamId, document.ProjectName, document.ConnectedByUserId,
        document.AutoSyncEnabled, document.LastSyncedOn, document.CreatedOn,
        document.History.Select(h => new SyncLogEntry(h.SyncedOn, h.CreatedCount, h.UpdatedCount, h.SkippedCount)));
}
