using MongoDB.Driver;
using TodoApp.Domain.Users;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<UserDocument> _users;

    public UserRepository(MongoDbContext context)
    {
        _users = context.Users;
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _users.Find(u => u.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var document = await _users.Find(u => u.Email == normalized).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _users.Find(u => u.Email == normalized).AnyAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _users.InsertOneAsync(ToDocument(user), cancellationToken: cancellationToken);

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default) =>
        await _users.ReplaceOneAsync(u => u.Id == user.Id, ToDocument(user), cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return Array.Empty<User>();

        var filter = Builders<UserDocument>.Filter.In(u => u.Id, idList);
        var documents = await _users.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    private static UserDocument ToDocument(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        PasswordHash = user.PasswordHash,
        CreatedOn = user.CreatedOn,
        NotifyOnAssignment = user.NotifyOnAssignment,
        NotifyOnMention = user.NotifyOnMention,
        NotifyOnInvitation = user.NotifyOnInvitation,
        IsEmailVerified = user.IsEmailVerified,
        NotifyOnDueSoon = user.NotifyOnDueSoon,
        ReminderLeadTimeHours = user.ReminderLeadTimeHours
    };

    private static User ToDomain(UserDocument document) => User.Rehydrate(
        document.Id, document.Email, document.DisplayName, document.PasswordHash, document.CreatedOn,
        document.NotifyOnAssignment ?? true, document.NotifyOnMention ?? true, document.NotifyOnInvitation ?? true,
        document.IsEmailVerified,
        document.NotifyOnDueSoon ?? true, document.ReminderLeadTimeHours ?? 24);
}
