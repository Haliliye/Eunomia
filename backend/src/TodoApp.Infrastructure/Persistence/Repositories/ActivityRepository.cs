using MongoDB.Driver;
using TodoApp.Domain.Activities;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class ActivityRepository : IActivityRepository
{
    private readonly IMongoCollection<ActivityDocument> _activities;

    public ActivityRepository(MongoDbContext context)
    {
        _activities = context.Activities;
    }

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default) =>
        await _activities.InsertOneAsync(ToDocument(activity), cancellationToken: cancellationToken);

    public async Task<(IReadOnlyList<Activity> Items, int TotalCount)> SearchByTeamIdAsync(
        string teamId, string? actorUserId, ActivityType? type, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<ActivityDocument>>
        {
            Builders<ActivityDocument>.Filter.Eq(a => a.TeamId, teamId)
        };

        if (!string.IsNullOrWhiteSpace(actorUserId))
            filters.Add(Builders<ActivityDocument>.Filter.Eq(a => a.ActorUserId, actorUserId));

        if (type.HasValue)
            filters.Add(Builders<ActivityDocument>.Filter.Eq(a => a.Type, type.Value.ToString()));

        var filter = Builders<ActivityDocument>.Filter.And(filters);

        var totalCount = (int)await _activities.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var documents = await _activities.Find(filter)
            .SortByDescending(a => a.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (documents.Select(ToDomain).ToList(), totalCount);
    }

    public async Task<IReadOnlyList<Activity>> GetByRelatedEntityIdAsync(string relatedEntityId, int limit, CancellationToken cancellationToken = default)
    {
        var documents = await _activities.Find(a => a.RelatedEntityId == relatedEntityId)
            .SortByDescending(a => a.CreatedOn)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(ToDomain).ToList();
    }

    private static ActivityDocument ToDocument(Activity activity) => new()
    {
        Id = activity.Id,
        TeamId = activity.TeamId,
        ActorUserId = activity.ActorUserId,
        Type = activity.Type.ToString(),
        Message = activity.Message,
        RelatedEntityId = activity.RelatedEntityId,
        CreatedOn = activity.CreatedOn
    };

    private static Activity ToDomain(ActivityDocument document) => Activity.Rehydrate(
        document.Id, document.TeamId, document.ActorUserId,
        Enum.Parse<ActivityType>(document.Type), document.Message, document.RelatedEntityId, document.CreatedOn);
}
