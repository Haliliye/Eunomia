using MongoDB.Driver;
using TodoApp.Domain.Auth;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshTokenDocument> _tokens;

    public RefreshTokenRepository(MongoDbContext context)
    {
        _tokens = context.RefreshTokens;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var document = await _tokens.Find(t => t.TokenHash == tokenHash).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        await _tokens.InsertOneAsync(ToDocument(token), cancellationToken: cancellationToken);

    public async Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        await _tokens.ReplaceOneAsync(t => t.Id == token.Id, ToDocument(token), cancellationToken: cancellationToken);

    private static RefreshTokenDocument ToDocument(RefreshToken token) => new()
    {
        Id = token.Id,
        UserId = token.UserId,
        TokenHash = token.TokenHash,
        ExpiresOn = token.ExpiresOn,
        CreatedOn = token.CreatedOn,
        RevokedOn = token.RevokedOn
    };

    private static RefreshToken ToDomain(RefreshTokenDocument document) => RefreshToken.Rehydrate(
        document.Id, document.UserId, document.TokenHash, document.ExpiresOn, document.CreatedOn, document.RevokedOn);
}
