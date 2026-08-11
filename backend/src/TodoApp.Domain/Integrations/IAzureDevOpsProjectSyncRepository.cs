namespace TodoApp.Domain.Integrations;

public interface IAzureDevOpsProjectSyncRepository
{
    Task<AzureDevOpsProjectSync?> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AzureDevOpsProjectSync>> GetAllAutoSyncEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AzureDevOpsProjectSync sync, CancellationToken cancellationToken = default);
    Task UpdateAsync(AzureDevOpsProjectSync sync, CancellationToken cancellationToken = default);
}
