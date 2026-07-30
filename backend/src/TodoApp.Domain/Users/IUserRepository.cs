namespace TodoApp.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Resolves multiple ids at once — used to show display names for team members/assignees instead of raw ids.</summary>
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}
