using MongoDB.Driver;
using TodoApp.Domain.Integrations;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class AzureDevOpsConnectionRepository : IAzureDevOpsConnectionRepository
{
    private readonly IMongoCollection<AzureDevOpsConnectionDocument> _connections;

    public AzureDevOpsConnectionRepository(MongoDbContext context)
    {
        _connections = context.AzureDevOpsConnections;
    }

    public async Task<AzureDevOpsConnection?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var document = await _connections.Find(c => c.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(AzureDevOpsConnection connection, CancellationToken cancellationToken = default) =>
        await _connections.InsertOneAsync(ToDocument(connection), cancellationToken: cancellationToken);

    public async Task UpdateAsync(AzureDevOpsConnection connection, CancellationToken cancellationToken = default) =>
        await _connections.ReplaceOneAsync(c => c.Id == connection.Id, ToDocument(connection), cancellationToken: cancellationToken);

    public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        await _connections.DeleteOneAsync(c => c.UserId == userId, cancellationToken);

    private static AzureDevOpsConnectionDocument ToDocument(AzureDevOpsConnection connection) => new()
    {
        Id = connection.Id,
        UserId = connection.UserId,
        OrganizationName = connection.OrganizationName,
        AccessTokenEncrypted = connection.AccessTokenEncrypted,
        RefreshTokenEncrypted = connection.RefreshTokenEncrypted,
        AccessTokenExpiresOn = connection.AccessTokenExpiresOn,
        ConnectedOn = connection.ConnectedOn,
    };

    private static AzureDevOpsConnection ToDomain(AzureDevOpsConnectionDocument document) => AzureDevOpsConnection.Rehydrate(
        document.Id, document.UserId, document.OrganizationName,
        document.AccessTokenEncrypted, document.RefreshTokenEncrypted, document.AccessTokenExpiresOn, document.ConnectedOn);
}
