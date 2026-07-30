using MongoDB.Driver;
using TodoApp.Domain.Invitations;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class InvitationRepository : IInvitationRepository
{
    private readonly IMongoCollection<InvitationDocument> _invitations;

    public InvitationRepository(MongoDbContext context)
    {
        _invitations = context.Invitations;
    }

    public async Task<Invitation?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _invitations.Find(i => i.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<Invitation>> GetPendingByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<InvitationDocument>.Filter.And(
            Builders<InvitationDocument>.Filter.Eq(i => i.InvitedUserId, userId),
            Builders<InvitationDocument>.Filter.Eq(i => i.Status, nameof(InvitationStatus.Pending)));

        var documents = await _invitations.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Invitation>> GetPendingByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<InvitationDocument>.Filter.And(
            Builders<InvitationDocument>.Filter.Eq(i => i.TeamId, teamId),
            Builders<InvitationDocument>.Filter.Eq(i => i.Status, nameof(InvitationStatus.Pending)));

        var documents = await _invitations.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<bool> HasPendingInvitationAsync(string teamId, string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<InvitationDocument>.Filter.And(
            Builders<InvitationDocument>.Filter.Eq(i => i.TeamId, teamId),
            Builders<InvitationDocument>.Filter.Eq(i => i.InvitedUserId, userId),
            Builders<InvitationDocument>.Filter.Eq(i => i.Status, nameof(InvitationStatus.Pending)));

        return await _invitations.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default) =>
        await _invitations.InsertOneAsync(ToDocument(invitation), cancellationToken: cancellationToken);

    public async Task UpdateAsync(Invitation invitation, CancellationToken cancellationToken = default) =>
        await _invitations.ReplaceOneAsync(i => i.Id == invitation.Id, ToDocument(invitation), cancellationToken: cancellationToken);

    private static InvitationDocument ToDocument(Invitation invitation) => new()
    {
        Id = invitation.Id,
        TeamId = invitation.TeamId,
        InvitedUserId = invitation.InvitedUserId,
        InvitedByUserId = invitation.InvitedByUserId,
        Status = invitation.Status.ToString(),
        CreatedOn = invitation.CreatedOn,
        RespondedOn = invitation.RespondedOn
    };

    private static Invitation ToDomain(InvitationDocument document) => Invitation.Rehydrate(
        document.Id, document.TeamId, document.InvitedUserId, document.InvitedByUserId,
        Enum.Parse<InvitationStatus>(document.Status), document.CreatedOn, document.RespondedOn);
}
