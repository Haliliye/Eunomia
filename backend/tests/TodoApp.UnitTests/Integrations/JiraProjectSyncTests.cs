using System.Linq;
using TodoApp.Domain.Integrations;
using Xunit;

namespace TodoApp.UnitTests.Integrations;

public class JiraProjectSyncTests
{
    [Fact]
    public void RecordSync_AddsANewestFirstHistoryEntryAndUpdatesLastSyncedOn()
    {
        var sync = JiraProjectSync.Create(Guid.NewGuid().ToString(), "team-1", "PROJ", "user-1");

        sync.RecordSync(5, 2, 1);

        Assert.NotNull(sync.LastSyncedOn);
        var entry = Assert.Single(sync.History);
        Assert.Equal(5, entry.CreatedCount);
        Assert.Equal(2, entry.UpdatedCount);
        Assert.Equal(1, entry.SkippedCount);
    }

    [Fact]
    public void RecordSync_CalledRepeatedly_KeepsMostRecentFirst()
    {
        var sync = JiraProjectSync.Create(Guid.NewGuid().ToString(), "team-1", "PROJ", "user-1");

        sync.RecordSync(1, 0, 0);
        sync.RecordSync(2, 0, 0);
        sync.RecordSync(3, 0, 0);

        Assert.Equal(new[] { 3, 2, 1 }, sync.History.Select(h => h.CreatedCount));
    }

    [Fact]
    public void RecordSync_MoreThanTenTimes_CapsHistoryAtTen()
    {
        var sync = JiraProjectSync.Create(Guid.NewGuid().ToString(), "team-1", "PROJ", "user-1");

        for (var i = 0; i < 15; i++)
            sync.RecordSync(i, 0, 0);

        Assert.Equal(10, sync.History.Count);
        // The most recent 10 calls used createdCount 14 down to 5.
        Assert.Equal(14, sync.History.First().CreatedCount);
        Assert.Equal(5, sync.History.Last().CreatedCount);
    }
}
