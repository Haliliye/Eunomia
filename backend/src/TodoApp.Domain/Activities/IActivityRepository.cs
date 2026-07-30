namespace TodoApp.Domain.Activities;

public interface IActivityRepository
{
    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);

    /// <summary>US-132/133: the team-wide feed, newest first, paginated, and
    /// optionally filtered by actor and/or type (both optional, combinable).</summary>
    Task<(IReadOnlyList<Activity> Items, int TotalCount)> SearchByTeamIdAsync(
        string teamId, string? actorUserId, ActivityType? type, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>US-131: a single user story's own activity history.</summary>
    Task<IReadOnlyList<Activity>> GetByRelatedEntityIdAsync(string relatedEntityId, int limit, CancellationToken cancellationToken = default);
}
