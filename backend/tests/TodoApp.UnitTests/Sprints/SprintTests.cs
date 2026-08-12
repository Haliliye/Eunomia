using TodoApp.Domain.Sprints;
using Xunit;

namespace TodoApp.UnitTests.Sprints;

/// <summary>
/// Pure domain coverage for Sprint's Planned → Active → Completed lifecycle
/// — previously untested at the domain level at all (only hit indirectly
/// through handler tests), per the 2026-08-11 review's "pure domain tests:
/// none" finding.
/// </summary>
public class SprintTests
{
    private static Sprint CreatePlannedSprint() =>
        Sprint.Create(Guid.NewGuid().ToString(), "team-1", "Sprint 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(14));

    [Fact]
    public void Create_EndDateNotAfterStartDate_ThrowsArgumentException()
    {
        var start = DateTime.UtcNow;
        Assert.Throws<ArgumentException>(() => Sprint.Create(Guid.NewGuid().ToString(), "team-1", "Sprint 1", start, start));
        Assert.Throws<ArgumentException>(() => Sprint.Create(Guid.NewGuid().ToString(), "team-1", "Sprint 1", start, start.AddDays(-1)));
    }

    [Fact]
    public void Create_BlankName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Sprint.Create(Guid.NewGuid().ToString(), "team-1", "  ", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Start_OnPlannedSprint_TransitionsToActive()
    {
        var sprint = CreatePlannedSprint();

        sprint.Start(totalPointsAtStart: 21);

        Assert.Equal(SprintStatus.Active, sprint.Status);
        Assert.Equal(21, sprint.TotalPointsAtStart);
    }

    [Fact]
    public void Start_OnAlreadyActiveSprint_ThrowsInvalidOperationException()
    {
        var sprint = CreatePlannedSprint();
        sprint.Start(21);

        Assert.Throws<InvalidOperationException>(() => sprint.Start(21));
    }

    [Fact]
    public void Start_OnCompletedSprint_ThrowsInvalidOperationException()
    {
        var sprint = CreatePlannedSprint();
        sprint.Start(21);
        sprint.Complete(21);

        Assert.Throws<InvalidOperationException>(() => sprint.Start(21));
    }

    [Fact]
    public void Complete_OnActiveSprint_TransitionsToCompleted()
    {
        var sprint = CreatePlannedSprint();
        sprint.Start(21);

        sprint.Complete(completedPoints: 18);

        Assert.Equal(SprintStatus.Completed, sprint.Status);
        Assert.Equal(18, sprint.CompletedPointsAtCompletion);
    }

    [Fact]
    public void Complete_OnPlannedSprint_ThrowsInvalidOperationException()
    {
        // A sprint can't be completed before it's ever started.
        var sprint = CreatePlannedSprint();

        Assert.Throws<InvalidOperationException>(() => sprint.Complete(0));
    }

    [Fact]
    public void Complete_OnAlreadyCompletedSprint_ThrowsInvalidOperationException()
    {
        var sprint = CreatePlannedSprint();
        sprint.Start(21);
        sprint.Complete(18);

        Assert.Throws<InvalidOperationException>(() => sprint.Complete(18));
    }
}
