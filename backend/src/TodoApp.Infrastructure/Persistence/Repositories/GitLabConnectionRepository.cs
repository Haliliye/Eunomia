using MongoDB.Driver;
using TodoApp.Domain.Integrations;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class GitLabConnectionRepository : IGitLabConnectionRepository
{
    private readonly IMongoCollection<GitLabConnectionDocument> _connections;

    public GitLabConnectionRepository(MongoDbContext context)
    {
        _connections = context.GitLabConnections;
    }

    public async Task<GitLabConnection?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var document = await _connections.Find(c => c.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(GitLabConnection connection, CancellationToken cancellationToken = default) =>
        await _connections.InsertOneAsync(ToDocument(connection), cancellationToken: cancellationToken);

    public async Task UpdateAsync(GitLabConnection connection, CancellationToken cancellationToken = default) =>
        await _connections.ReplaceOneAsync(c => c.Id == connection.Id, ToDocument(connection), cancellationToken: cancellationToken);

    public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        await _connections.DeleteOneAsync(c => c.UserId == userId, cancellationToken);

    private static GitLabConnectionDocument ToDocument(GitLabConnection connection) => new()
    {
        Id = connection.Id,
        UserId = connection.UserId,
        GitLabUsername = connection.GitLabUsername,
        AccessTokenEncrypted = connection.AccessTokenEncrypted,
        RefreshTokenEncrypted = connection.RefreshTokenEncrypted,
        AccessTokenExpiresOn = connection.AccessTokenExpiresOn,
        ConnectedOn = connection.ConnectedOn,
    };

    private static GitLabConnection ToDomain(GitLabConnectionDocument document) => GitLabConnection.Rehydrate(
        document.Id, document.UserId, document.GitLabUsername, document.AccessTokenEncrypted,
        document.RefreshTokenEncrypted, document.AccessTokenExpiresOn, document.ConnectedOn);
}
