namespace TodoApp.Domain.Integrations;

public interface IAzureDevOpsConnectionRepository
{
    Task<AzureDevOpsConnection?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(AzureDevOpsConnection connection, CancellationToken cancellationToken = default);
    Task UpdateAsync(AzureDevOpsConnection connection, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
