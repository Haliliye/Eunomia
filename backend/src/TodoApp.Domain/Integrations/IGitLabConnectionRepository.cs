namespace TodoApp.Domain.Integrations;

public interface IGitLabConnectionRepository
{
    Task<GitLabConnection?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(GitLabConnection connection, CancellationToken cancellationToken = default);
    Task UpdateAsync(GitLabConnection connection, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
