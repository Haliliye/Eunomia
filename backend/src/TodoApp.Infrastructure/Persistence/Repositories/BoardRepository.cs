using MongoDB.Driver;
using TodoApp.Domain.Boards;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly IMongoCollection<BoardDocument> _boards;

    public BoardRepository(MongoDbContext context)
    {
        _boards = context.Boards;
    }

    public async Task<Board?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _boards.Find(b => b.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<Board>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var documents = await _boards.Find(b => b.TeamId == teamId).SortBy(b => b.CreatedOn).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Board board, CancellationToken cancellationToken = default) =>
        await _boards.InsertOneAsync(ToDocument(board), cancellationToken: cancellationToken);

    public async Task UpdateAsync(Board board, CancellationToken cancellationToken = default) =>
        await _boards.ReplaceOneAsync(b => b.Id == board.Id, ToDocument(board), cancellationToken: cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _boards.DeleteOneAsync(b => b.Id == id, cancellationToken);

    private static BoardDocument ToDocument(Board board) => new()
    {
        Id = board.Id,
        TeamId = board.TeamId,
        Name = board.Name,
        SprintId = board.SprintId,
        CreatedOn = board.CreatedOn,
    };

    private static Board ToDomain(BoardDocument document) =>
        Board.Rehydrate(document.Id, document.TeamId, document.Name, document.SprintId, document.CreatedOn);
}
