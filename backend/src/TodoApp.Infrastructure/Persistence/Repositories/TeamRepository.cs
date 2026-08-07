using MongoDB.Driver;
using TodoApp.Domain.Teams;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly IMongoCollection<TeamDocument> _teams;

    public TeamRepository(MongoDbContext context)
    {
        _teams = context.Teams;
    }

    public async Task<Team?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _teams.Find(t => t.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<Team>> GetByMemberIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TeamDocument>.Filter.ElemMatch(t => t.Members, m => m.UserId == userId);
        var documents = await _teams.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<Team> Items, int TotalCount)> SearchByMemberIdAsync(
        string userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TeamDocument>.Filter.ElemMatch(t => t.Members, m => m.UserId == userId);

        var totalCount = (int)await _teams.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var documents = await _teams.Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (documents.Select(ToDomain).ToList(), totalCount);
    }

    public async Task<bool> ExistsWithNameForUserAsync(string name, string ownerId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TeamDocument>.Filter.And(
            Builders<TeamDocument>.Filter.Eq(t => t.Name, name),
            Builders<TeamDocument>.Filter.ElemMatch(t => t.Members,
                m => m.UserId == ownerId && m.Role == nameof(TeamRole.Owner)));

        return await _teams.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Team team, CancellationToken cancellationToken = default) =>
        await _teams.InsertOneAsync(ToDocument(team), cancellationToken: cancellationToken);

    public async Task UpdateAsync(Team team, CancellationToken cancellationToken = default) =>
        await _teams.ReplaceOneAsync(t => t.Id == team.Id, ToDocument(team), cancellationToken: cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _teams.DeleteOneAsync(t => t.Id == id, cancellationToken);

    // --- Mapping between the Team aggregate and its plain persistence document ---

    private static TeamDocument ToDocument(Team team) => new()
    {
        Id = team.Id,
        Name = team.Name,
        Description = team.Description,
        Members = team.Members.Select(m => new TeamMemberDocument
        {
            UserId = m.UserId,
            Role = m.Role.ToString(),
            JoinedOn = m.JoinedOn
        }).ToList(),
        Labels = team.Labels.Select(l => new LabelDocument { Id = l.Id, Name = l.Name, Color = l.Color }).ToList(),
        WipLimits = team.WipLimits.Select(w => new ColumnWipLimitDocument { Status = w.Status, Limit = w.Limit }).ToList(),
        Templates = team.Templates.Select(t => new StoryTemplateDocument
        {
            Id = t.Id, Name = t.Name, DefaultDescription = t.DefaultDescription, DefaultPriority = t.DefaultPriority,
            ChecklistItemTexts = t.ChecklistItemTexts.ToList()
        }).ToList(),
        Columns = team.Columns.Select(c => new BoardColumnDocument { Key = c.Key, Name = c.Name, Order = c.Order }).ToList()
    };

    private static Team ToDomain(TeamDocument document) => Team.Rehydrate(
        document.Id,
        document.Name,
        document.Description,
        document.Members.Select(m => new TeamMember(m.UserId, Enum.Parse<TeamRole>(m.Role), m.JoinedOn)),
        document.Labels.Select(l => new Label(l.Id, l.Name, l.Color)),
        document.WipLimits.Select(w => new ColumnWipLimit(w.Status, w.Limit)),
        document.Templates.Select(t => new StoryTemplate(t.Id, t.Name, t.DefaultDescription, t.DefaultPriority, t.ChecklistItemTexts)),
        document.Columns.Select(c => new BoardColumn(c.Key, c.Name, c.Order)));
}
