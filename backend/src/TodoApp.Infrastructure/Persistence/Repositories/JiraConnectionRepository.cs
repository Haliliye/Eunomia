using MongoDB.Driver;
using TodoApp.Domain.Integrations;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class JiraConnectionRepository : IJiraConnectionRepository
{
    private readonly IMongoCollection<JiraConnectionDocument> _connections;

    public JiraConnectionRepository(MongoDbContext context)
    {
        _connections = context.JiraConnections;
    }

    public async Task<JiraConnection?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var document = await _connections.Find(c => c.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(JiraConnection connection, CancellationToken cancellationToken = default) =>
        await _connections.InsertOneAsync(ToDocument(connection), cancellationToken: cancellationToken);

    public async Task UpdateAsync(JiraConnection connection, CancellationToken cancellationToken = default) =>
        await _connections.ReplaceOneAsync(c => c.Id == connection.Id, ToDocument(connection), cancellationToken: cancellationToken);

    public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        await _connections.DeleteOneAsync(c => c.UserId == userId, cancellationToken);

    private static JiraConnectionDocument ToDocument(JiraConnection connection) => new()
    {
        Id = connection.Id,
        UserId = connection.UserId,
        CloudId = connection.CloudId,
        SiteUrl = connection.SiteUrl,
        SiteName = connection.SiteName,
        AccessTokenEncrypted = connection.AccessTokenEncrypted,
        RefreshTokenEncrypted = connection.RefreshTokenEncrypted,
        AccessTokenExpiresOn = connection.AccessTokenExpiresOn,
        ConnectedOn = connection.ConnectedOn,
    };

    private static JiraConnection ToDomain(JiraConnectionDocument document) => JiraConnection.Rehydrate(
        document.Id, document.UserId, document.CloudId, document.SiteUrl, document.SiteName,
        document.AccessTokenEncrypted, document.RefreshTokenEncrypted, document.AccessTokenExpiresOn, document.ConnectedOn);
}
