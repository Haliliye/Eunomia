namespace TodoApp.Domain.Invitations;

public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invitation>> GetPendingByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invitation>> GetPendingByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingInvitationAsync(string teamId, string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invitation invitation, CancellationToken cancellationToken = default);
}
