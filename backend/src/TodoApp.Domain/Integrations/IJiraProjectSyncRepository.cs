namespace TodoApp.Domain.Integrations;

public interface IJiraProjectSyncRepository
{
    Task<JiraProjectSync?> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JiraProjectSync>> GetAllAutoSyncEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(JiraProjectSync sync, CancellationToken cancellationToken = default);
    Task UpdateAsync(JiraProjectSync sync, CancellationToken cancellationToken = default);
}
