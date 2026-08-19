using MongoDB.Driver;
using TodoApp.Domain.Integrations;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class GitHubConnectionRepository : IGitHubConnectionRepository
{
    private readonly IMongoCollection<GitHubConnectionDocument> _connections;

    public GitHubConnectionRepository(MongoDbContext context)
    {
        _connections = context.GitHubConnections;
    }

    public async Task<GitHubConnection?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var document = await _connections.Find(c => c.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(GitHubConnection connection, CancellationToken cancellationToken = default) =>
        await _connections.InsertOneAsync(ToDocument(connection), cancellationToken: cancellationToken);

    public async Task UpdateAsync(GitHubConnection connection, CancellationToken cancellationToken = default) =>
        await _connections.ReplaceOneAsync(c => c.Id == connection.Id, ToDocument(connection), cancellationToken: cancellationToken);

    public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        await _connections.DeleteOneAsync(c => c.UserId == userId, cancellationToken);

    private static GitHubConnectionDocument ToDocument(GitHubConnection connection) => new()
    {
        Id = connection.Id,
        UserId = connection.UserId,
        AccessTokenEncrypted = connection.AccessTokenEncrypted,
        GitHubLogin = connection.GitHubLogin,
        ConnectedOn = connection.ConnectedOn,
    };

    private static GitHubConnection ToDomain(GitHubConnectionDocument document) => GitHubConnection.Rehydrate(
        document.Id, document.UserId, document.AccessTokenEncrypted, document.GitHubLogin, document.ConnectedOn);
}
