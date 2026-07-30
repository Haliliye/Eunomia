using MongoDB.Bson.Serialization.Attributes;

namespace TodoApp.Infrastructure.Persistence.Documents;

public class SprintDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public int? TotalPointsAtStart { get; set; }
    public int? CompletedPointsAtCompletion { get; set; }
    public List<BurndownSnapshotDocument> BurndownSnapshots { get; set; } = new();
}

public class BurndownSnapshotDocument
{
    public DateOnly Date { get; set; }
    public int RemainingCount { get; set; }
    public int RemainingPoints { get; set; }
}
