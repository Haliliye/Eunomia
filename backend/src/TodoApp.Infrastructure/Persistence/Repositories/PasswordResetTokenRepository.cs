using MongoDB.Driver;
using TodoApp.Domain.Auth;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IMongoCollection<PasswordResetTokenDocument> _tokens;

    public PasswordResetTokenRepository(MongoDbContext context)
    {
        _tokens = context.PasswordResetTokens;
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var document = await _tokens.Find(t => t.TokenHash == tokenHash).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default) =>
        await _tokens.InsertOneAsync(ToDocument(token), cancellationToken: cancellationToken);

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default) =>
        await _tokens.ReplaceOneAsync(t => t.Id == token.Id, ToDocument(token), cancellationToken: cancellationToken);

    private static PasswordResetTokenDocument ToDocument(PasswordResetToken token) => new()
    {
        Id = token.Id,
        UserId = token.UserId,
        TokenHash = token.TokenHash,
        ExpiresOn = token.ExpiresOn,
        CreatedOn = token.CreatedOn,
        UsedOn = token.UsedOn
    };

    private static PasswordResetToken ToDomain(PasswordResetTokenDocument document) => PasswordResetToken.Rehydrate(
        document.Id, document.UserId, document.TokenHash, document.ExpiresOn, document.CreatedOn, document.UsedOn);
}
