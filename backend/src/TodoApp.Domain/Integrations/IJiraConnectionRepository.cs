namespace TodoApp.Domain.Integrations;

public interface IJiraConnectionRepository
{
    Task<JiraConnection?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(JiraConnection connection, CancellationToken cancellationToken = default);
    Task UpdateAsync(JiraConnection connection, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
