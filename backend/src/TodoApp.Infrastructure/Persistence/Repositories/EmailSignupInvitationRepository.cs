using MongoDB.Driver;
using TodoApp.Domain.Invitations;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class EmailSignupInvitationRepository : IEmailSignupInvitationRepository
{
    private readonly IMongoCollection<EmailSignupInvitationDocument> _invitations;

    public EmailSignupInvitationRepository(MongoDbContext context)
    {
        _invitations = context.EmailSignupInvitations;
    }

    public async Task<IReadOnlyList<EmailSignupInvitation>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var documents = await _invitations.Find(i => i.Email == normalized).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<bool> ExistsAsync(string email, string teamId, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _invitations.Find(i => i.Email == normalized && i.TeamId == teamId).AnyAsync(cancellationToken);
    }

    public async Task AddAsync(EmailSignupInvitation invitation, CancellationToken cancellationToken = default) =>
        await _invitations.InsertOneAsync(ToDocument(invitation), cancellationToken: cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _invitations.DeleteOneAsync(i => i.Id == id, cancellationToken);

    private static EmailSignupInvitationDocument ToDocument(EmailSignupInvitation invitation) => new()
    {
        Id = invitation.Id,
        Email = invitation.Email,
        TeamId = invitation.TeamId,
        InvitedByUserId = invitation.InvitedByUserId,
        CreatedOn = invitation.CreatedOn,
    };

    private static EmailSignupInvitation ToDomain(EmailSignupInvitationDocument document) =>
        EmailSignupInvitation.Rehydrate(document.Id, document.Email, document.TeamId, document.InvitedByUserId, document.CreatedOn);
}
