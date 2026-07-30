namespace TodoApp.Domain.Sprints;

public interface ISprintRepository
{
    Task<Sprint?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sprint>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task<Sprint?> GetActiveByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task AddAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default);
}
