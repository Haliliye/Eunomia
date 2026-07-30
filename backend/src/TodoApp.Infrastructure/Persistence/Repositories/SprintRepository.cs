using MongoDB.Driver;
using TodoApp.Domain.Sprints;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class SprintRepository : ISprintRepository
{
    private readonly IMongoCollection<SprintDocument> _sprints;

    public SprintRepository(MongoDbContext context)
    {
        _sprints = context.Sprints;
    }

    public async Task<Sprint?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _sprints.Find(s => s.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<Sprint>> GetByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var documents = await _sprints.Find(s => s.TeamId == teamId).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<Sprint?> GetActiveByTeamIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SprintDocument>.Filter.And(
            Builders<SprintDocument>.Filter.Eq(s => s.TeamId, teamId),
            Builders<SprintDocument>.Filter.Eq(s => s.Status, nameof(SprintStatus.Active)));

        var document = await _sprints.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(Sprint sprint, CancellationToken cancellationToken = default) =>
        await _sprints.InsertOneAsync(ToDocument(sprint), cancellationToken: cancellationToken);

    public async Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default) =>
        await _sprints.ReplaceOneAsync(s => s.Id == sprint.Id, ToDocument(sprint), cancellationToken: cancellationToken);

    private static SprintDocument ToDocument(Sprint sprint) => new()
    {
        Id = sprint.Id,
        TeamId = sprint.TeamId,
        Name = sprint.Name,
        StartDate = sprint.StartDate,
        EndDate = sprint.EndDate,
        Status = sprint.Status.ToString(),
        CreatedOn = sprint.CreatedOn,
        TotalPointsAtStart = sprint.TotalPointsAtStart,
        CompletedPointsAtCompletion = sprint.CompletedPointsAtCompletion,
        BurndownSnapshots = sprint.BurndownSnapshots
            .Select(s => new BurndownSnapshotDocument { Date = s.Date, RemainingCount = s.RemainingCount, RemainingPoints = s.RemainingPoints })
            .ToList()
    };

    private static Sprint ToDomain(SprintDocument document) => Sprint.Rehydrate(
        document.Id, document.TeamId, document.Name, document.StartDate, document.EndDate,
        Enum.Parse<SprintStatus>(document.Status), document.CreatedOn,
        document.TotalPointsAtStart,
        document.BurndownSnapshots.Select(s => new BurndownSnapshot(s.Date, s.RemainingCount, s.RemainingPoints)),
        document.CompletedPointsAtCompletion);
}
