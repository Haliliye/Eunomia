namespace TodoApp.Domain.Teams;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Team>> GetByMemberIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Team> Items, int TotalCount)> SearchByMemberIdAsync(
        string userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNameForUserAsync(string name, string ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(Team team, CancellationToken cancellationToken = default);
    Task UpdateAsync(Team team, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
