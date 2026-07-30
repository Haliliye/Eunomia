using TodoApp.Domain.Common;

namespace TodoApp.Domain.Activities;

/// <summary>
/// A single "who did what" entry for a team's activity feed (US-131/132/133).
/// Message stays a flat human-readable string (this is for reading, not
/// programmatic replay) — Type exists specifically so US-133's "filter by
/// action type" has something structured to filter on.
/// </summary>
public class Activity : AggregateRoot
{
    public string TeamId { get; private set; } = string.Empty;
    public string ActorUserId { get; private set; } = string.Empty;
    public ActivityType Type { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? RelatedEntityId { get; private set; }
    public DateTime CreatedOn { get; private set; }

    private Activity() { }

    private Activity(string id, string teamId, string actorUserId, ActivityType type, string message, string? relatedEntityId) : base(id)
    {
        TeamId = teamId;
        ActorUserId = actorUserId;
        Type = type;
        Message = message;
        RelatedEntityId = relatedEntityId;
        CreatedOn = DateTime.UtcNow;
    }

    public static Activity Create(string id, string teamId, string actorUserId, ActivityType type, string message, string? relatedEntityId = null) =>
        new(id, teamId, actorUserId, type, message, relatedEntityId);

    public static Activity Rehydrate(string id, string teamId, string actorUserId, ActivityType type, string message, string? relatedEntityId, DateTime createdOn)
    {
        var activity = new Activity(id, teamId, actorUserId, type, message, relatedEntityId) { CreatedOn = createdOn };
        return activity;
    }
}
