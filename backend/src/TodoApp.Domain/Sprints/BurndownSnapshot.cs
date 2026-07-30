namespace TodoApp.Domain.Sprints;

/// <summary>One day's snapshot of remaining work for a sprint's burndown chart —
/// taken at most once per calendar day (see Sprint.RecordSnapshot).</summary>
public class BurndownSnapshot
{
    public DateOnly Date { get; private set; }
    public int RemainingCount { get; private set; }
    public int RemainingPoints { get; private set; }

    private BurndownSnapshot() { }

    public BurndownSnapshot(DateOnly date, int remainingCount, int remainingPoints)
    {
        Date = date;
        RemainingCount = remainingCount;
        RemainingPoints = remainingPoints;
    }
}
