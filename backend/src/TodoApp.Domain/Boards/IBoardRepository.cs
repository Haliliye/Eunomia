namespace TodoApp.Domain.Boards;

public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Board>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default);
    Task AddAsync(Board board, CancellationToken cancellationToken = default);
    Task UpdateAsync(Board board, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
