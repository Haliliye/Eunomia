using Moq;
using TodoApp.Application.Common;
using TodoApp.Application.Sprints.Commands.CompleteSprint;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using Xunit;

namespace TodoApp.UnitTests.Sprints;

public class CompleteSprintCommandHandlerTests
{
    [Fact]
    public async Task Handle_MixOfDoneAndNotDoneStories_ReturnsAccurateSummaryAndMovesCarriedOverToBacklog()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        var sprint = Sprint.Create(Guid.NewGuid().ToString(), team.Id, "Sprint 1", DateTime.UtcNow.AddDays(-14), DateTime.UtcNow);
        sprint.Start(10);

        var doneStory = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Done story", null);
        doneStory.ChangeStatus("Done");
        doneStory.SetStoryPoints(5);
        doneStory.MoveToSprint(sprint.Id);

        var notDoneStory = UserStory.Create(Guid.NewGuid().ToString(), team.Id, "Unfinished story", null);
        notDoneStory.ChangeStatus("Dev");
        notDoneStory.SetStoryPoints(3);
        notDoneStory.MoveToSprint(sprint.Id);

        var sprintRepoMock = new Mock<ISprintRepository>();
        sprintRepoMock.Setup(r => r.GetByIdAsync(sprint.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sprint);
        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var storyRepoMock = new Mock<IUserStoryRepository>();
        storyRepoMock.Setup(r => r.SearchAsync(
            team.Id, null, null, null, null, 1, 500, false, sprint.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserStory> { doneStory, notDoneStory }, 2));
        var realtimeMock = new Mock<IRealtimeNotifier>();

        var handler = new CompleteSprintCommandHandler(sprintRepoMock.Object, storyRepoMock.Object, teamRepoMock.Object, realtimeMock.Object);

        var summary = await handler.Handle(new CompleteSprintCommand(sprint.Id, "owner-1"), CancellationToken.None);

        Assert.Equal(1, summary.CompletedCount);
        Assert.Equal(5, summary.CompletedPoints);
        Assert.Equal(1, summary.CarriedOverCount);
        Assert.Equal(3, summary.CarriedOverPoints);
        Assert.Equal(notDoneStory.Id, Assert.Single(summary.CarriedOverStories).Id);
        Assert.Null(notDoneStory.SprintId); // rolled back to the backlog
        Assert.Equal(sprint.Id, doneStory.SprintId); // a Done story stays put, it's not touched by the rollover
        Assert.Equal(SprintStatus.Completed, sprint.Status);
    }

    [Fact]
    public async Task Handle_RequesterIsOrdinaryMember_ThrowsUnauthorizedAccessException()
    {
        var team = Team.Create(Guid.NewGuid().ToString(), "Platform Team", null, "owner-1");
        team.AddMember("member-1", "owner-1");
        var sprint = Sprint.Create(Guid.NewGuid().ToString(), team.Id, "Sprint 1", DateTime.UtcNow.AddDays(-14), DateTime.UtcNow);
        sprint.Start(0);

        var sprintRepoMock = new Mock<ISprintRepository>();
        sprintRepoMock.Setup(r => r.GetByIdAsync(sprint.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sprint);
        var teamRepoMock = new Mock<ITeamRepository>();
        teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var storyRepoMock = new Mock<IUserStoryRepository>();
        var realtimeMock = new Mock<IRealtimeNotifier>();

        var handler = new CompleteSprintCommandHandler(sprintRepoMock.Object, storyRepoMock.Object, teamRepoMock.Object, realtimeMock.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new CompleteSprintCommand(sprint.Id, "member-1"), CancellationToken.None));
    }
}
