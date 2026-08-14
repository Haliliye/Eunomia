using Moq;
using TodoApp.Application.Teams.Queries.GetPortfolioOverview;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using Xunit;

namespace TodoApp.UnitTests.Teams;

public class GetPortfolioOverviewQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExcludesArchivedAndSubtasksFromCounts()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");

        var doneStory = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Done story", null);
        doneStory.ChangeStatus("Done");

        var openStory = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Open story", null);

        var archivedStory = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Archived story", null);
        archivedStory.Archive();

        var subtask = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "A subtask", null, parentId: doneStory.Id);

        var overdueStory = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Overdue story", null);
        overdueStory.SetDueDate(DateTime.UtcNow.AddDays(-3));

        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByMemberIdAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Team> { team });
        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByTeamIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserStory> { doneStory, openStory, archivedStory, subtask, overdueStory });
        var sprintRepoMock = new Mock<ISprintRepository>();
        sprintRepoMock.Setup(r => r.GetActiveByTeamIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Sprint?)null);

        var handler = new GetPortfolioOverviewQueryHandler(teamRepoMock.Object, storyRepoMock.Object, sprintRepoMock.Object);

        var result = await handler.Handle(new GetPortfolioOverviewQuery("owner-1"), CancellationToken.None);

        var row = Assert.Single(result);
        Assert.Equal(team.Name, row.TeamName);
        Assert.Equal(1, row.MemberCount);
        // archivedStory and subtask are excluded — only doneStory, openStory, overdueStory count.
        Assert.Equal(3, row.TotalStoryCount);
        Assert.Equal(1, row.DoneCount);
        Assert.Equal(1, row.OverdueCount);
        Assert.Null(row.ActiveSprintName);
    }

    [Fact]
    public async Task Handle_IncludesActiveSprintWhenOneExists()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var sprint = Sprint.Create(Guid.NewGuid().ToString(), team.Id, "Sprint 7", DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(4));
        sprint.Start(0);

        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByMemberIdAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Team> { team });
        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.GetByTeamIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserStory>());
        var sprintRepoMock = new Mock<ISprintRepository>();
        sprintRepoMock.Setup(r => r.GetActiveByTeamIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sprint);

        var handler = new GetPortfolioOverviewQueryHandler(teamRepoMock.Object, storyRepoMock.Object, sprintRepoMock.Object);

        var result = await handler.Handle(new GetPortfolioOverviewQuery("owner-1"), CancellationToken.None);

        var row = Assert.Single(result);
        Assert.Equal("Sprint 7", row.ActiveSprintName);
        Assert.Equal(sprint.EndDate, row.ActiveSprintEndDate);
    }
}
