using TodoApp.Domain.Common;

namespace TodoApp.Domain.Sprints;

/// <summary>
/// A time-boxed iteration a team plans work into. Stories reference a
/// sprint by id (UserStory.SprintId, nullable — null means "still in the
/// backlog"); the sprint itself doesn't hold a list of story ids, avoiding
/// two sources of truth for "which sprint is this story in".
/// </summary>
public class Sprint : AggregateRoot
{
    public string TeamId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public SprintStatus Status { get; private set; } = SprintStatus.Planned;
    public DateTime CreatedOn { get; private set; }

    /// <summary>Captured once, when the sprint starts (see Start(Overload)) — the
    /// "ideal" burndown line is drawn from this value down to 0 across the
    /// sprint's date range. Null until the sprint has actually started.</summary>
    public int? TotalPointsAtStart { get; private set; }

    /// <summary>Captured once, when the sprint completes — how many story
    /// points were actually Done by then. Together with TotalPointsAtStart,
    /// this is what a team velocity chart plots across sprints.</summary>
    public int? CompletedPointsAtCompletion { get; private set; }

    private readonly List<BurndownSnapshot> _burndownSnapshots = new();
    public IReadOnlyList<BurndownSnapshot> BurndownSnapshots => _burndownSnapshots.OrderBy(s => s.Date).ToList();

    private Sprint() { }

    private Sprint(string id, string teamId, string name, DateTime startDate, DateTime endDate) : base(id)
    {
        TeamId = teamId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        CreatedOn = DateTime.UtcNow;
    }

    public static Sprint Create(string id, string teamId, string name, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sprint name is required.", nameof(name));

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after the start date.", nameof(endDate));

        return new Sprint(id, teamId, name.Trim(), startDate, endDate);
    }

    public static Sprint Rehydrate(
        string id, string teamId, string name, DateTime startDate, DateTime endDate, SprintStatus status, DateTime createdOn,
        int? totalPointsAtStart = null, IEnumerable<BurndownSnapshot>? burndownSnapshots = null, int? completedPointsAtCompletion = null)
    {
        var sprint = new Sprint(id, teamId, name, startDate, endDate)
        {
            Status = status,
            CreatedOn = createdOn,
            TotalPointsAtStart = totalPointsAtStart,
            CompletedPointsAtCompletion = completedPointsAtCompletion
        };
        if (burndownSnapshots is not null) sprint._burndownSnapshots.AddRange(burndownSnapshots);
        return sprint;
    }

    /// <summary>totalPointsAtStart is supplied by the caller (StartSprintCommandHandler)
    /// since computing it requires querying UserStoryRepository, which this aggregate
    /// deliberately has no access to.</summary>
    public void Start(int totalPointsAtStart)
    {
        if (Status != SprintStatus.Planned)
            throw new InvalidOperationException($"Only a Planned sprint can be started (this one is {Status}).");

        Status = SprintStatus.Active;
        TotalPointsAtStart = totalPointsAtStart;
    }

    public void Complete(int completedPoints)
    {
        if (Status != SprintStatus.Active)
            throw new InvalidOperationException($"Only an Active sprint can be completed (this one is {Status}).");

        Status = SprintStatus.Completed;
        CompletedPointsAtCompletion = completedPoints;
    }

    /// <summary>At most one snapshot per calendar day — calling this again on the
    /// same day just overwrites that day's numbers with the latest figures rather
    /// than adding a second entry, so repeated dashboard views don't pollute the chart.</summary>
    public void RecordSnapshot(DateOnly date, int remainingCount, int remainingPoints)
    {
        var existing = _burndownSnapshots.FirstOrDefault(s => s.Date == date);
        if (existing is not null) _burndownSnapshots.Remove(existing);

        _burndownSnapshots.Add(new BurndownSnapshot(date, remainingCount, remainingPoints));
    }
}
