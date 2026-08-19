namespace TodoApp.Domain.Integrations;

public interface IGitHubConnectionRepository
{
    Task<GitHubConnection?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(GitHubConnection connection, CancellationToken cancellationToken = default);
    Task UpdateAsync(GitHubConnection connection, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
