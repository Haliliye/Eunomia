using MongoDB.Driver;
using TodoApp.Domain.Auth;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly IMongoCollection<EmailVerificationTokenDocument> _tokens;

    public EmailVerificationTokenRepository(MongoDbContext context)
    {
        _tokens = context.EmailVerificationTokens;
    }

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var document = await _tokens.Find(t => t.TokenHash == tokenHash).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default) =>
        await _tokens.InsertOneAsync(ToDocument(token), cancellationToken: cancellationToken);

    public async Task UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default) =>
        await _tokens.ReplaceOneAsync(t => t.Id == token.Id, ToDocument(token), cancellationToken: cancellationToken);

    private static EmailVerificationTokenDocument ToDocument(EmailVerificationToken token) => new()
    {
        Id = token.Id,
        UserId = token.UserId,
        TokenHash = token.TokenHash,
        ExpiresOn = token.ExpiresOn,
        CreatedOn = token.CreatedOn,
        UsedOn = token.UsedOn
    };

    private static EmailVerificationToken ToDomain(EmailVerificationTokenDocument document) => EmailVerificationToken.Rehydrate(
        document.Id, document.UserId, document.TokenHash, document.ExpiresOn, document.CreatedOn, document.UsedOn);
}
