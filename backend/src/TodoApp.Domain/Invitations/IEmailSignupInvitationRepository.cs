namespace TodoApp.Domain.Invitations;

public interface IEmailSignupInvitationRepository
{
    Task<IReadOnlyList<EmailSignupInvitation>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string email, string teamId, CancellationToken cancellationToken = default);
    Task AddAsync(EmailSignupInvitation invitation, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
